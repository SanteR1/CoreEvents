using CoreEvents.Shared.Contracts.Events;
using Events.Application.Abstractions;
using Events.Application.Abstractions.Caching;
using Events.Application.Abstractions.Repositories;
using Events.Application.DTOs;
using Events.Application.Exceptions;
using Events.Domain.Entities;

namespace Events.Application.Services;

internal sealed class EventService : IEventService
{
    private readonly IEventRepository _eventRepository;
    private readonly ICacheService _cache;
    private readonly IOutboxService _outboxService;

    public EventService(
        IEventRepository eventRepository,
        ICacheService cache,
        IOutboxService outboxService)
    {
        _eventRepository = eventRepository;
        _cache = cache;
        _outboxService = outboxService;
    }

    public async Task<PaginatedResult<EventResponseDto>> GetAllEventsAsync(EventFilter dtoFilter, CancellationToken ct = default)
    {
        DateTime? startInclusive = null;
        if (dtoFilter.From is { } fromDate)
        {
            startInclusive = fromDate.Kind switch
            {
                // Если пришло локальное время — конвертируем его в UTC
                DateTimeKind.Local => fromDate.ToUniversalTime(),

                // Если пришло Unspecified — вешаем ярлык UTC
                DateTimeKind.Unspecified => DateTime.SpecifyKind(fromDate, DateTimeKind.Utc),

                // Если уже UTC — ничего не делаем
                DateTimeKind.Utc => fromDate,

                _ => fromDate
            };
        }

        DateTime? endExclusive = null;

        if (dtoFilter.To is { } toDate)
        {
            // 1. Сначала проверяем намерения пользователя по ЕГО времени
            bool isFullDay = toDate.TimeOfDay == TimeSpan.Zero;

            // 2. Безопасно приводим саму дату к UTC, не искажая часы, если это Unspecified
            DateTime utcToDate = toDate.Kind switch
            {
                DateTimeKind.Local => toDate.ToUniversalTime(),
                DateTimeKind.Unspecified => DateTime.SpecifyKind(toDate, DateTimeKind.Utc),
                DateTimeKind.Utc => toDate,
                _ => toDate
            };

            // 3. Добавляем сдвиги к уже нормализованной UTC-дате
            if (isFullDay)
            {
                // Пользователь просил весь день (до конца суток)
                endExclusive = utcToDate.AddDays(1);
            }
            else
            {
                // Пользователь передал точное время, добавляем микросекунду для БД
                endExclusive = utcToDate.AddMicroseconds(1);
            }
        }

        int pageSize = dtoFilter.PageSize > 0
            ? Math.Min(dtoFilter.PageSize, 100)
            : 10;
        int page = dtoFilter.Page > 0
            ? Math.Min(dtoFilter.Page, 100000)
            : 1;

        var eventFilter = dtoFilter with { From = startInclusive, To = endExclusive, PageSize = pageSize, Page = page };
        var pagedEvents = await _eventRepository.GetAllAsync(eventFilter, ct);
        return pagedEvents.Map(EventResponseDto.FromEntity);
    }

    public async Task<EventResponseDto?> GetEventByIdAsync(Guid id, CancellationToken ct = default)
    {
        var cachedDto = await _cache.GetAsync<EventCacheDto>(CacheKeys.Event(id), ct);
        if (cachedDto != null)
            return EventResponseDto.FromEntity(cachedDto);

        var eventEntity = await _eventRepository.GetByIdAsync(id, ct);
        if (eventEntity == null)
            throw new EventNotFoundException(id);

        await _cache.SetAsync<EventCacheDto>(CacheKeys.Event(id), EventCacheDto.FromEntity(eventEntity), ct);

        return EventResponseDto.FromEntity(eventEntity);
    }

    public async Task<IReadOnlyList<EventResponseDto>> GetTopEventsBySalesPercentageAsync(CancellationToken ct = default)
    {
        const int top = 10;
        var existKey = await _cache.GetAsync<IReadOnlyList<EventCacheDto>>(CacheKeys.Top10Events, ct);

        if (existKey != null)
            return EventResponseDto.FromEntity(existKey);

        var eventEntity = await _eventRepository.GetTopEventsBySalesPercentageAsync(top, ct);

        if (eventEntity.Count > 0)
        {
            await _cache.SetAsync<IReadOnlyList<EventCacheDto>>(CacheKeys.Top10Events, EventCacheDto.FromEntity(eventEntity), ct);
        }

        return EventResponseDto.FromEntity(eventEntity);
    }

    public async Task<EventResponseDto> CreateEventAsync(EventCreateDto createDto, CancellationToken ct = default)
    {
        var entity = Event.Create(
            title: createDto.Title!,
            startAt: createDto.StartAt!.Value,
            endAt: createDto.EndAt!.Value,
            totalSeats: createDto.TotalSeats!.Value,
            description: createDto.Description,
            price: createDto.Price,
            currency: createDto.Currency);

        _eventRepository.Add(entity);

        _outboxService.Publish(
            new EventCreated
            {
                EventId = entity.Id,
                Title = entity.Title,
                StartAt = entity.StartAt,
                EndAt = entity.EndAt,
                UnitPrice = entity.Price,
                Currency = entity.Currency,
                PriceVersion = entity.PriceVersion,
                TotalSeats = entity.TotalSeats,
                IsActive = entity.IsActive,
                Version = entity.Version,
                CreatedAt = DateTimeOffset.UtcNow
            },
            partitionKey: entity.Id.ToString());

        await _eventRepository.SaveChangesAsync(ct);

        return EventResponseDto.FromEntity(entity);
    }

    public async Task<EventResponseDto> UpdateEventAsync(Guid id, EventUpdateDto updateDto, CancellationToken ct = default)
    {
        var existing = await _eventRepository.GetByIdAsync(id, ct);
        if (existing == null) throw new EventNotFoundException(id);

        existing.Update(
            updateDto.Title,
            updateDto.StartAt,
            updateDto.EndAt,
            updateDto.Description,
            updateDto.Price,
            updateDto.Currency);

        _outboxService.Publish(
            new EventUpdated
            {
                EventId = existing.Id,
                Title = existing.Title,
                Description = existing.Description,
                StartAt = existing.StartAt,
                EndAt = existing.EndAt,
                UnitPrice = existing.Price,
                Currency = existing.Currency,
                PriceVersion = existing.PriceVersion,
                TotalSeats = existing.TotalSeats,
                IsActive = existing.IsActive,
                Version = existing.Version,
                UpdatedAt = DateTimeOffset.UtcNow
            },
            partitionKey: existing.Id.ToString());

        await _eventRepository.SaveChangesAsync(ct);

        await _cache.DeleteAsync(CacheKeys.Event(id), ct);

        return EventResponseDto.FromEntity(existing);
    }

    public async Task<bool> DeleteEventAsync(Guid id, CancellationToken ct = default)
    {
        var existing = await _eventRepository.GetByIdAsync(id, ct);
        if (existing == null) throw new EventNotFoundException(id);

        existing.Cancel();

        _outboxService.Publish(
            new EventCancelled
            {
                EventId = existing.Id,
                Reason = "Cancelled by organizer",
                Version = existing.Version,
                CancelledAt = DateTimeOffset.UtcNow
            },
            partitionKey: existing.Id.ToString());

        await _eventRepository.SaveChangesAsync(ct);

        await _cache.DeleteAsync(CacheKeys.Event(id), ct);

        return true;
    }
}
