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
public record BookingReservationReleaseRequestCommand(
    Guid BookingId,
    Guid EventId,
    int Seats,
    CancellationReason Reason,
    InboxEnvelope Inbox) : IInboxCommand
{
    public BookingReservationReleaseRequestCommand(Guid bookingId, Guid eventId, int seats, CancellationReason reason)
        : this(bookingId, eventId, seats, reason, InboxEnvelope.CreateDefault("booking.reservation-release-requested.v1")) { }
}

internal class BookingReservationReleaseRequestHandler(
    IEventRepository eventRepository,
    ISeatReservationRepository reservationRepository,
    IOutboxService outboxService,
    ICorrelationContext correlationContext) : IRequestHandler<BookingReservationReleaseRequestCommand, InboxProcessingResult>
{
    public async Task<InboxProcessingResult> Handle(BookingReservationReleaseRequestCommand request, CancellationToken cancellationToken)
    {
        var correlationId = correlationContext.CorrelationId != Guid.Empty ? correlationContext.CorrelationId : request.BookingId;
        var causationId = correlationContext.CausationId ?? Guid.NewGuid();

        var reservation = await reservationRepository.GetByBookingIdAsync(request.BookingId, cancellationToken);
        if (reservation == null)
        {
            await HandleMissingReservationAsync(request, correlationId, causationId, cancellationToken);
            return InboxProcessingResult.Processed;
        }

        if (reservation.Status == SeatsReservationStatus.Reserved)
        {
            await ReleaseReservedSeatsAsync(request, reservation, cancellationToken);
            return InboxProcessingResult.Processed;
        }

        PublishReleasedEvent(request);
        return InboxProcessingResult.Processed;
    }

    private async Task HandleMissingReservationAsync(
        BookingReservationReleaseRequestCommand request,
        Guid correlationId,
        Guid causationId,
        CancellationToken ct)
    {
        var tombstone = SeatReservation.CreateCancelledBeforeReservation(
            request.BookingId,
            request.EventId,
            request.Seats,
            correlationId,
            causationId);

        reservationRepository.Add(tombstone);
        await reservationRepository.SaveChangesAsync(ct);
        PublishReleasedEvent(request);
    }

    private async Task ReleaseReservedSeatsAsync(
        BookingReservationReleaseRequestCommand request,
        SeatReservation reservation,
        CancellationToken ct)
    {
        reservation.Release();
        var @event = await eventRepository.GetByIdAsync(request.EventId, ct);
        if (@event != null)
        {
            @event.ReleaseSeats(request.BookingId, request.Seats, DateTimeOffset.UtcNow);
        }

        await reservationRepository.SaveChangesAsync(ct);
        PublishReleasedEvent(request);
    }

    private void PublishReleasedEvent(BookingReservationReleaseRequestCommand request)
    {
        outboxService.AddIntegrationEvent(
            new BookingReservationReleased
            {
                BookingId = request.BookingId,
                EventId = request.EventId,
                Seats = request.Seats,
                ReleasedAt = DateTimeOffset.UtcNow
            },
            partitionKey: request.EventId.ToString());
    }
}
