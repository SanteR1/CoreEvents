using Events.Application.Abstractions.Caching;
using Events.Application.Abstractions.Messaging;
using Events.Domain.DomainEvents;

namespace Events.Application.Events;

internal sealed class InvalidateEventCacheImmediateHandler(
    ICacheService cacheService)
    : IPostCommitHandler<SeatsReleasedDomainEvent>,
      IPostCommitHandler<SeatsReservedDomainEvent>,
      IPostCommitHandler<EventCreatedDomainEvent>,
      IPostCommitHandler<EventCancelledDomainEvent>,
      IPostCommitHandler<EventUpdatedDomainEvent>
{
    public Task Handle(SeatsReleasedDomainEvent notification, CancellationToken ct)
    {
        return cacheService.DeleteAsync(CacheKeys.Event(notification.EventId), ct);
    }

    public Task Handle(SeatsReservedDomainEvent notification, CancellationToken ct)
    {
        return cacheService.DeleteAsync(CacheKeys.Event(notification.EventId), ct);
    }

    public Task Handle(EventCreatedDomainEvent notification, CancellationToken ct)
    {
        return cacheService.DeleteAsync(CacheKeys.Event(notification.EventId), ct);
    }

    public Task Handle(EventCancelledDomainEvent notification, CancellationToken ct)
    {
        return cacheService.DeleteAsync(CacheKeys.Event(notification.EventId), ct);
    }
    public Task Handle(EventUpdatedDomainEvent notification, CancellationToken ct)
    {
        return cacheService.DeleteAsync(CacheKeys.Event(notification.EventId), ct);
    }
}
