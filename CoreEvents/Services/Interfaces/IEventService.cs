using CoreEvents.Models.Domain;
using CoreEvents.Models.DTOs;

namespace CoreEvents.Services.Interfaces
{
    public interface IEventService
    {
        ValueTask<PaginatedResult> GetEvents(EventFilter eventFilter);
        ValueTask<EventResponseDto> GetEventById(Guid id);
        ValueTask<EventResponseDto> CreateEvent(EventCreateDto entityDto);
        ValueTask UpdateEvent(Guid id, EventCreateDto entityDto);
        ValueTask DeleteEvent(Guid id);
    }
}
