using CoreEvents.Models.DTOs;

namespace CoreEvents.Services.Interfaces
{
    public interface IEventService
    {
        Task<PaginatedResult<EventResponseDto>> GetAllEventsAsync(EventFilter dtoFilter, CancellationToken ct = default);
        Task<EventResponseDto?> GetEventByIdAsync(Guid id, CancellationToken ct = default);
        Task<EventResponseDto> CreateEventAsync(EventCreateDto createDto, CancellationToken ct = default);
        Task<EventResponseDto> UpdateEventAsync(Guid id, EventUpdateDto updateDto, CancellationToken ct = default);
        Task<bool> DeleteEventAsync(Guid id, CancellationToken ct = default);
    }
}
