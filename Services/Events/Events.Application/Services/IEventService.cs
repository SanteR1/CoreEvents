using Events.Application.DTOs;

namespace Events.Application.Services;

public interface IEventService
{
    Task<PaginatedResult<EventResponseDto>> GetAllEventsAsync(EventFilter dtoFilter, CancellationToken ct = default);
    Task<EventResponseDto?> GetEventByIdAsync(Guid id, CancellationToken ct = default);
    Task<List<EventResponseDto>> GetTopEventsBySalesPercentageAsync(CancellationToken ct = default);
    Task<EventResponseDto> CreateEventAsync(EventCreateDto createDto, CancellationToken ct = default);
    Task<EventResponseDto> UpdateEventAsync(Guid id, EventUpdateDto updateDto, CancellationToken ct = default);
    Task<bool> DeleteEventAsync(Guid id, CancellationToken ct = default);
}
