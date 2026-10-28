using Events.Application.Abstractions;
using Events.Application.Abstractions.Messaging;
using Events.Domain.DomainEvents;

namespace Events.Application.Events;

internal sealed class InvalidateEventCacheHandler(
    IOutboxService outboxService)
    : IPreCommitHandler<SeatsReleased>,
      IPreCommitHandler<SeatsReserved>,
      IPreCommitHandler<EventCreated>,
      IPreCommitHandler<EventCancelled>
{
    public Task Handle(SeatsReleased notification, CancellationToken ct)
    {
        outboxService.AddCacheInvalidation(notification.EventId);
        return Task.CompletedTask;
    }

    public Task Handle(SeatsReserved notification, CancellationToken ct)
    {
        outboxService.AddCacheInvalidation(notification.EventId);
        return Task.CompletedTask;
    }

    public Task Handle(EventCreated notification, CancellationToken ct)
    {
        outboxService.AddCacheInvalidation(notification.EventId);
        return Task.CompletedTask;
    }

    public Task Handle(EventCancelled notification, CancellationToken ct)
    {
        outboxService.AddCacheInvalidation(notification.EventId);
        return Task.CompletedTask;
    }
}
