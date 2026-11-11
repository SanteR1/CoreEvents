using Events.Application.Abstractions;
using Events.Application.Abstractions.Messaging;
using Events.Domain.DomainEvents;

namespace Events.Application.Events;

internal sealed class InvalidateEventCacheHandler(
    IOutboxService outboxService)
    : IPreCommitHandler<SeatsReleasedDomainEvent>,
      IPreCommitHandler<SeatsReservedDomainEvent>,
      IPreCommitHandler<EventCreatedDomainEvent>,
      IPreCommitHandler<EventUpdatedDomainEvent>,
      IPreCommitHandler<EventCancelledDomainEvent>
{
    public Task Handle(SeatsReleasedDomainEvent notification, CancellationToken ct)
    {
        outboxService.AddCacheInvalidation(notification.EventId);
        return Task.CompletedTask;
    }

    public Task Handle(SeatsReservedDomainEvent notification, CancellationToken ct)
    {
        outboxService.AddCacheInvalidation(notification.EventId);
        return Task.CompletedTask;
    }

    public Task Handle(EventCreatedDomainEvent notification, CancellationToken ct)
    {
        outboxService.AddCacheInvalidation(notification.EventId);
        return Task.CompletedTask;
    }

    public Task Handle(EventUpdatedDomainEvent notification, CancellationToken ct)
    {
        outboxService.AddCacheInvalidation(notification.EventId);
        return Task.CompletedTask;
    }
    public Task Handle(EventCancelledDomainEvent notification, CancellationToken ct)
    {
        outboxService.AddCacheInvalidation(notification.EventId);
        return Task.CompletedTask;
    }
}
