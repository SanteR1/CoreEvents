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
public record BookingReservationReleaseRequestCommand(Guid BookingId, Guid EventId, int Seats, CancellationReason Reason) : ICommand<Unit>;

internal class BookingReservationReleaseRequestHandler(
    IEventRepository eventRepository,
    ISeatReservationRepository reservationRepository,
    IOutboxService outboxService,
    ICorrelationContext correlationContext) : IRequestHandler<BookingReservationReleaseRequestCommand, Unit>
{
    public async Task<Unit> Handle(BookingReservationReleaseRequestCommand request, CancellationToken cancellationToken)
    {
        var correlationId = correlationContext.CorrelationId != Guid.Empty ? correlationContext.CorrelationId : request.BookingId;
        var causationId = correlationContext.CausationId ?? Guid.NewGuid();

        var reservation = await reservationRepository.GetByBookingIdAsync(request.BookingId, cancellationToken);
        if (reservation == null)
        {
            await HandleMissingReservationAsync(request, correlationId, causationId, cancellationToken);
            return Unit.Value;
        }

        if (reservation.Status == SeatReservationStatus.Reserved)
        {
            await ReleaseReservedSeatsAsync(request, reservation, cancellationToken);
            return Unit.Value;
        }

        PublishReleasedEvent(request);
        return Unit.Value;
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
            @event.ReleaseSeats(request.Seats);
        }

        await reservationRepository.SaveChangesAsync(ct);
        PublishReleasedEvent(request);
    }

    private void PublishReleasedEvent(BookingReservationReleaseRequestCommand request)
    {
        outboxService.Publish(
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
