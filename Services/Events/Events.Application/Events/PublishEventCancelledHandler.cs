using CoreEvents.Shared.Contracts.Events;
using Events.Application.Abstractions;
using Events.Application.Abstractions.Messaging;
using Events.Domain.DomainEvents;

namespace Events.Application.Events;

internal sealed class PublishEventCancelledHandler(
    IOutboxService outboxService) : IPreCommitHandler<EventCancelledDomainEvent>
{
    public Task Handle(EventCancelledDomainEvent e, CancellationToken ct)
    {
        outboxService.AddIntegrationEvent(
            new EventCancelled
            {
                EventId = e.EventId,
                Version = e.Version,
                CancelledAt = e.OccurredAt
            },
            partitionKey: e.EventId.ToString());
        return Task.CompletedTask;
    }
}
