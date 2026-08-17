using Events.Application.Abstractions.Caching;
using Events.Application.Abstractions.Messaging;
using Events.Domain.DomainEvents;
using MediatR;

namespace Events.Application.Events;

internal sealed class InvalidateEventCacheHandler : INotificationHandler<DomainEventNotification>
{
    private readonly ICacheService _cacheService;

    public InvalidateEventCacheHandler(ICacheService cacheService)
    {
        _cacheService = cacheService;
    }

    public async Task Handle(
        DomainEventNotification notification,
        CancellationToken cancellationToken)
    {
        if (notification.DomainEvent is ICacheInvalidationEvent cacheEvent)
            await _cacheService.DeleteAsync(CacheKeys.Event(cacheEvent.EventId), cancellationToken);
    }
}
