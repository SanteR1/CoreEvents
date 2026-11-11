using Events.Application.Abstractions.Caching;
using Events.Application.Abstractions.Messaging;
using Events.Application.Abstractions.Repositories;
using Events.Application.DTOs;
using Events.Application.Exceptions;

namespace Events.Application.Queries;

public sealed record GetEventByIdQuery(Guid id) : IQuery<EventResponseDto?>;

internal sealed class GetEventByIdQueryHandler(IEventRepository eventRepository, ICacheService cache) : IQueryHandler<GetEventByIdQuery, EventResponseDto?>
{
    public async Task<EventResponseDto?> Handle(GetEventByIdQuery request, CancellationToken cancellationToken)
    {
        var cachedDto = await cache.GetAsync<EventCacheDto>(CacheKeys.Event(request.id), cancellationToken);
        if (cachedDto != null)
            return EventResponseDto.FromEntity(cachedDto);

        var eventEntity = await eventRepository.GetByIdAsync(request.id, cancellationToken);
        if (eventEntity == null)
            throw new EventNotFoundException(request.id);

        await cache.SetAsync<EventCacheDto>(CacheKeys.Event(request.id), EventCacheDto.FromEntity(eventEntity), cancellationToken);

        return EventResponseDto.FromEntity(eventEntity);
    }
}
