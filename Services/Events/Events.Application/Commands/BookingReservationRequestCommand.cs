using CoreEvents.Shared.Contracts.Events;
using Events.Application.Abstractions;
using Events.Application.Abstractions.Messaging;
using Events.Application.Abstractions.Repositories;
using Events.Application.Abstractions.Resilience.Attributes;
using Events.Application.Abstractions.Resilience.Constants;
using Events.Application.Common.Mappers;
using Events.Domain.Entities;
using Events.Domain.Enums;
using MediatR;

namespace Events.Application.Commands;

[ResiliencePipeline(ResiliencePipelines.CommandConcurrency)]
public record BookingReservationRequestCommand(
    Guid BookingId,
    Guid EventId,
    Guid UserId,
    int Seats,
    InboxEnvelope Inbox) : IInboxCommand
{
    public BookingReservationRequestCommand(Guid bookingId, Guid eventId, Guid userId, int seats)
        : this(bookingId, eventId, userId, seats, InboxEnvelope.CreateDefault("booking.reservation-requested.v1")) { }
}

internal class BookingReservationRequestHandler(
    IEventRepository eventRepository,
    ISeatReservationRepository reservationRepository,
    IOutboxService outboxService,
    ICorrelationContext correlationContext) : IRequestHandler<BookingReservationRequestCommand, InboxProcessingResult>
{
    public async Task<InboxProcessingResult> Handle(BookingReservationRequestCommand request, CancellationToken cancellationToken)
    {
        var existingReservation = await reservationRepository.GetByBookingIdAsync(request.BookingId, cancellationToken);
        if (existingReservation != null && TryHandleExistingReservation(request, existingReservation))
        {
            return InboxProcessingResult.Processed;
        }

        var correlationId = correlationContext.CorrelationId != Guid.Empty ? correlationContext.CorrelationId : request.BookingId;
        var causationId = correlationContext.CausationId ?? Guid.NewGuid();

        var @event = await eventRepository.GetByIdAsync(request.EventId, cancellationToken);
        if (@event == null || !@event.IsActive)
        {
            RejectReservation(request, ValidationFailureReason.EventNotFound, correlationId, causationId);
            return InboxProcessingResult.Processed;
        }

        if (@event.StartAt <= DateTime.UtcNow)
        {
            RejectReservation(request, ValidationFailureReason.EventAlreadyPassed, correlationId, causationId);
            return InboxProcessingResult.Processed;
        }

        var reserveResult = @event.TryReserveSeats(request.BookingId, request.Seats, DateTimeOffset.UtcNow);
        if (reserveResult != ReserveSeatsResult.Reserved)
        {
            RejectReservation(request, reserveResult.ToValidationFailureReason(), correlationId, causationId);
            return InboxProcessingResult.Processed;
        }

        AcceptReservation(request, @event, correlationId, causationId);
        return InboxProcessingResult.Processed;
    }

    private bool TryHandleExistingReservation(BookingReservationRequestCommand request, SeatReservation existingReservation)
    {
        if (existingReservation.Status == SeatsReservationStatus.Reserved)
        {
            outboxService.AddIntegrationEvent(
                new BookingReservationAccepted
                {
                    BookingId = existingReservation.BookingId,
                    EventId = existingReservation.EventId,
                    Seats = existingReservation.Seats,
                    UnitPrice = existingReservation.UnitPrice,
                    TotalPrice = existingReservation.TotalPrice,
                    Currency = existingReservation.Currency,
                    DiscountAmount = existingReservation.DiscountAmount,
                    PriceVersion = existingReservation.PriceVersion,
                    ExpiresAt = existingReservation.ExpiresAt
                },
                partitionKey: request.EventId.ToString());
            return true;
        }

        if (existingReservation.Status == SeatsReservationStatus.CancelledBeforeReservation ||
            existingReservation.Status == SeatsReservationStatus.Rejected)
        {
            outboxService.AddIntegrationEvent(
                new BookingReservationRejected
                {
                    BookingId = request.BookingId,
                    EventId = request.EventId,
                    RejectionReason = existingReservation.RejectionReason ?? ValidationFailureReason.SeatsNotAvailable
                },
                partitionKey: request.EventId.ToString());
            return true;
        }

        return false;
    }

    private void RejectReservation(
        BookingReservationRequestCommand request,
        ValidationFailureReason reason,
        Guid correlationId,
        Guid causationId)
    {
        var rejected = SeatReservation.CreateRejected(request.BookingId, request.EventId, request.Seats, reason, correlationId, causationId);
        reservationRepository.Add(rejected);

        outboxService.AddIntegrationEvent(
            new BookingReservationRejected
            {
                BookingId = request.BookingId,
                EventId = request.EventId,
                RejectionReason = reason
            },
            partitionKey: request.EventId.ToString());
    }

    private void AcceptReservation(
        BookingReservationRequestCommand request,
        Event @event,
        Guid correlationId,
        Guid causationId)
    {
        var expiresAt = DateTimeOffset.UtcNow.AddMinutes(15);
        var reservation = SeatReservation.CreateReserved(
            request.BookingId,
            request.EventId,
            request.Seats,
            @event.Price,
            @event.Currency,
            discountAmount: 0.00m,
            @event.PriceVersion,
            expiresAt,
            correlationId,
            causationId);

        reservationRepository.Add(reservation);

        outboxService.AddIntegrationEvent(
            new BookingReservationAccepted
            {
                BookingId = reservation.BookingId,
                EventId = reservation.EventId,
                Seats = reservation.Seats,
                UnitPrice = reservation.UnitPrice,
                TotalPrice = reservation.TotalPrice,
                Currency = reservation.Currency,
                DiscountAmount = reservation.DiscountAmount,
                PriceVersion = reservation.PriceVersion,
                ExpiresAt = reservation.ExpiresAt
            },
            partitionKey: request.EventId.ToString());
    }
}
