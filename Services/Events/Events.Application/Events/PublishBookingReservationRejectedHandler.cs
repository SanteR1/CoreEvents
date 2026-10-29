using CoreEvents.Shared.Contracts.Events;
using Events.Application.Abstractions;
using Events.Application.Abstractions.Messaging;
using Events.Domain.DomainEvents;
using Events.Domain.Enums;

namespace Events.Application.Events;

internal sealed class PublishBookingReservationRejectedHandler(
    IOutboxService outboxService)
    : IPreCommitHandler<SeatsReservationRejected>
{
    public Task Handle(SeatsReservationRejected e, CancellationToken ct)
    {
        var failureReason = e.Reason == SeatsReservationRejectionReason.EventInactive
            ? ValidationFailureReason.EventNotFound
            : ValidationFailureReason.SeatsNotAvailable;

        outboxService.AddIntegrationEvent(
            new BookingReservationRejected
            {
                BookingId = e.BookingId,
                EventId = e.EventId,
                RejectionReason = failureReason
            },
            partitionKey: e.EventId.ToString());

        return Task.CompletedTask;
    }
}
