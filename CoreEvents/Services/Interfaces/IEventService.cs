using CoreEvents.Models.DTOs;

namespace CoreEvents.Services.Interfaces
{
    public interface IEventService
    {
        Task<PaginatedResult> GetAllEventsAsync(EventFilter eventFilter, CancellationToken ct = default);
        Task<EventResponseDto> GetEventByIdAsync(Guid id, CancellationToken ct = default);
        Task<EventResponseDto> CreateEventAsync(EventCreateDto entityDto, CancellationToken ct = default);
        Task<EventResponseDto> UpdateEventAsync(Guid id, EventUpdateDto entityDto, CancellationToken ct = default);
        Task<bool> DeleteEventAsync(Guid id, CancellationToken ct = default);
    }
}
