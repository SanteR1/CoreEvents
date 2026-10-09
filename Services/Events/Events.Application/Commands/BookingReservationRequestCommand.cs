using CoreEvents.Shared.Contracts.Events;
using Events.Application.Abstractions;
using Events.Application.Abstractions.Messaging;
using Events.Application.Abstractions.Repositories;
using Events.Application.Abstractions.Resilience.Attributes;
using Events.Application.Abstractions.Resilience.Constants;
using Events.Domain.Entities;
using Events.Domain.Enums;
using MediatR;

namespace Events.Application.Commands;

[ResiliencePipeline(ResiliencePipelines.CommandConcurrency)]
public record BookingReservationRequestCommand(Guid BookingId, Guid EventId, Guid UserId, int Seats) : ICommand<Unit>;

internal class BookingReservationRequestHandler(
    IEventRepository eventRepository,
    ISeatReservationRepository reservationRepository,
    IOutboxService outboxService,
    ICorrelationContext correlationContext) : IRequestHandler<BookingReservationRequestCommand, Unit>
{
    public async Task<Unit> Handle(BookingReservationRequestCommand request, CancellationToken cancellationToken)
    {
        var existingReservation = await reservationRepository.GetByBookingIdAsync(request.BookingId, cancellationToken);
        if (existingReservation != null && TryHandleExistingReservation(request, existingReservation))
        {
            return Unit.Value;
        }

        var correlationId = correlationContext.CorrelationId != Guid.Empty ? correlationContext.CorrelationId : request.BookingId;
        var causationId = correlationContext.CausationId ?? Guid.NewGuid();

        var @event = await eventRepository.GetByIdAsync(request.EventId, cancellationToken);
        if (@event == null || !@event.IsActive)
        {
            await RejectReservationAsync(request, ValidationFailureReason.EventNotFound, correlationId, causationId, cancellationToken);
            return Unit.Value;
        }

        if (@event.StartAt <= DateTime.UtcNow)
        {
            await RejectReservationAsync(request, ValidationFailureReason.EventAlreadyPassed, correlationId, causationId, cancellationToken);
            return Unit.Value;
        }

        if (!@event.TryReserveSeats(request.Seats))
        {
            await RejectReservationAsync(request, ValidationFailureReason.SeatsNotAvailable, correlationId, causationId, cancellationToken);
            return Unit.Value;
        }

        await AcceptReservationAsync(request, @event, correlationId, causationId, cancellationToken);
        return Unit.Value;
    }

    private bool TryHandleExistingReservation(BookingReservationRequestCommand request, SeatReservation existingReservation)
    {
        if (existingReservation.Status == SeatReservationStatus.Reserved)
        {
            outboxService.Publish(
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

        if (existingReservation.Status == SeatReservationStatus.CancelledBeforeReservation ||
            existingReservation.Status == SeatReservationStatus.Rejected)
        {
            outboxService.Publish(
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

    private async Task RejectReservationAsync(
        BookingReservationRequestCommand request,
        ValidationFailureReason reason,
        Guid correlationId,
        Guid causationId,
        CancellationToken ct)
    {
        var rejected = SeatReservation.CreateRejected(request.BookingId, request.EventId, request.Seats, reason, correlationId, causationId);
        reservationRepository.Add(rejected);
        await reservationRepository.SaveChangesAsync(ct);

        outboxService.Publish(
            new BookingReservationRejected
            {
                BookingId = request.BookingId,
                EventId = request.EventId,
                RejectionReason = reason
            },
            partitionKey: request.EventId.ToString());
    }

    private async Task AcceptReservationAsync(
        BookingReservationRequestCommand request,
        Event @event,
        Guid correlationId,
        Guid causationId,
        CancellationToken ct)
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
        await eventRepository.SaveChangesAsync(ct);

        outboxService.Publish(
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
