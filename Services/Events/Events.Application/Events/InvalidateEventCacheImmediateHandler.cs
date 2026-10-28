using Events.Application.Abstractions.Caching;
using Events.Application.Abstractions.Messaging;
using Events.Domain.DomainEvents;

namespace Events.Application.Events;

internal sealed class InvalidateEventCacheImmediateHandler(
    ICacheService cacheService)
    : IPostCommitHandler<SeatsReleased>,
      IPostCommitHandler<SeatsReserved>,
      IPostCommitHandler<EventCreated>,
      IPostCommitHandler<EventCancelled>
{
    public Task Handle(SeatsReleased notification, CancellationToken ct)
    {
        return cacheService.DeleteAsync(CacheKeys.Event(notification.EventId), ct);
    }

    public Task Handle(SeatsReserved notification, CancellationToken ct)
    {
        return cacheService.DeleteAsync(CacheKeys.Event(notification.EventId), ct);
    }

    public Task Handle(EventCreated notification, CancellationToken ct)
    {
        return cacheService.DeleteAsync(CacheKeys.Event(notification.EventId), ct);
    }

    public Task Handle(EventCancelled notification, CancellationToken ct)
    {
        return cacheService.DeleteAsync(CacheKeys.Event(notification.EventId), ct);
    }
}
