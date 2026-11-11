
using Events.Application.Abstractions.Caching;
using Events.Application.Abstractions.Messaging;
using Events.Application.Abstractions.Repositories;
using Events.Application.DTOs;

namespace Events.Application.Queries;

public sealed record GetTopEventsQuery() : IQuery<IReadOnlyList<EventResponseDto>>;

internal sealed class GetTopEventsQueryHandler(IEventRepository eventRepository, ICacheService cache) : IQueryHandler<GetTopEventsQuery, IReadOnlyList<EventResponseDto>>
{
    public async Task<IReadOnlyList<EventResponseDto>> Handle(GetTopEventsQuery _, CancellationToken cancellationToken)
    {
        const int top = 10;
        var existKey = await cache.GetAsync<IReadOnlyList<EventCacheDto>>(CacheKeys.Top10Events, cancellationToken);

        if (existKey != null)
            return EventResponseDto.FromEntity(existKey);

        var eventEntity = await eventRepository.GetTopEventsBySalesPercentageAsync(top, cancellationToken);

        if (eventEntity.Count > 0)
        {
            await cache.SetAsync<IReadOnlyList<EventCacheDto>>(CacheKeys.Top10Events, EventCacheDto.FromEntity(eventEntity), cancellationToken);
        }

        return EventResponseDto.FromEntity(eventEntity);
    }
}
