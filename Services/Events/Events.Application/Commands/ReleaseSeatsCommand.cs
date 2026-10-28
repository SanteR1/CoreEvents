using CoreEvents.Shared.Contracts.Events;
using CoreEvents.Shared.Contracts.Exceptions;
using Events.Application.Abstractions;
using Events.Application.Abstractions.Messaging;
using Events.Application.Abstractions.Repositories;
using Events.Application.Abstractions.Resilience.Attributes;
using Events.Application.Abstractions.Resilience.Constants;
using Events.Domain.Policies;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Events.Application.Commands;

[ResiliencePipeline(ResiliencePipelines.CommandConcurrency)]
public record ReleaseSeatsCommand(
    Guid BookingId,
    Guid EventId,
    int Seats,
    InboxEnvelope Inbox) : IInboxCommand
{
    public ReleaseSeatsCommand(Guid bookingId, Guid eventId, int seats)
        : this(bookingId, eventId, seats, InboxEnvelope.CreateDefault("booking.cancellation-requested.v1")) { }
}

internal class ReleaseSeatsHandler(
    IEventRepository events,
    ISeatReservationRepository reservations,
    IOutboxService outbox,
    ILogger<ReleaseSeatsHandler> logger) : IRequestHandler<ReleaseSeatsCommand, InboxProcessingResult>
{
    public async Task<InboxProcessingResult> Handle(ReleaseSeatsCommand r, CancellationToken cancellationToken)
    {
        var reservation = await reservations.GetByBookingIdAsync(r.BookingId, cancellationToken);
        var seatsReleased = false;

        if (reservation is null)
        {
            logger.LogWarning("No reservation for booking {BookingId}. Refusing blind release.", r.BookingId);
        }
        else
        {
            var @event = await events.GetByIdAsync(r.EventId, cancellationToken);
            var outcome = SeatReleasePolicy.Apply(reservation, @event, DateTimeOffset.UtcNow);

            if (outcome.IsIntegrityViolation())
            {
                logger.LogError("Integrity violation {Outcome}: booking {BookingId}, event {EventId}.",
                    outcome, r.BookingId, r.EventId);
                throw new DataIntegrityException($"{outcome}: booking {r.BookingId}, event {r.EventId}");
            }

            seatsReleased = outcome is ReleaseOutcome.Released or ReleaseOutcome.AlreadyReleased;
        }

        outbox.AddIntegrationEvent(new EventBookingCancellationCompleted
        {
            BookingId = r.BookingId,
            EventId = r.EventId,
            SeatsReleased = seatsReleased
        },
        partitionKey: r.EventId.ToString());

        return InboxProcessingResult.Processed;
    }
}
