using CoreEvents.Shared.Contracts.Exceptions;
using Events.Application.Abstractions.Messaging;
using Events.Application.Abstractions.Repositories;
using Events.Application.Abstractions.Resilience.Attributes;
using Events.Application.Abstractions.Resilience.Constants;
using Events.Domain.Policies;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Events.Application.Commands;

[ResiliencePipeline(ResiliencePipelines.CommandConcurrency)]
public record SyncBookingCancelledCommand(
    Guid BookingId,
    Guid EventId,
    InboxEnvelope Inbox) : IInboxCommand
{
    public SyncBookingCancelledCommand(Guid bookingId, Guid eventId)
        : this(bookingId, eventId, InboxEnvelope.CreateDefault("booking.cancelled.v1")) { }
}

internal class SyncBookingCancelledHandler(
    IEventRepository events,
    ISeatReservationRepository reservations,
    ILogger<SyncBookingCancelledHandler> logger) : IRequestHandler<SyncBookingCancelledCommand, InboxProcessingResult>
{
    public async Task<InboxProcessingResult> Handle(SyncBookingCancelledCommand r, CancellationToken cancellationToken)
    {
        var reservation = await reservations.GetByBookingIdAsync(r.BookingId, cancellationToken);
        if (reservation is null)
        {
            logger.LogInformation("No reservation for booking {BookingId}. No-op.", r.BookingId);
            return InboxProcessingResult.Processed;
        }

        var @event = await events.GetByIdAsync(r.EventId, cancellationToken);
        var outcome = SeatReleasePolicy.Apply(reservation, @event, DateTimeOffset.UtcNow);

        if (outcome.IsIntegrityViolation())
        {
            logger.LogError("Integrity violation {Outcome}: booking {BookingId}, event {EventId}.",
                outcome, r.BookingId, r.EventId);
            throw new DataIntegrityException($"{outcome}: booking {r.BookingId}, event {r.EventId}");
        }

        if (outcome == ReleaseOutcome.AlreadyReleased)
        {
            logger.LogInformation("Booking {BookingId}: AlreadyReleased. No-op.", r.BookingId);
        }
        else if (outcome == ReleaseOutcome.Released)
        {
            logger.LogInformation("Booking {BookingId}: seats released successfully.", r.BookingId);
        }
        else
        {
            logger.LogInformation("Booking {BookingId}: {Outcome}. No-op.", r.BookingId, outcome);
        }

        return InboxProcessingResult.Processed;
    }
}
