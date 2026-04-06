using CoreEvents.Models.Domain;
using CoreEvents.Models.DTOs;

namespace CoreEvents.Services
{
    public interface IEventService
    {
        IEnumerable<EventResponseDto> GetEvents();
        EventResponseDto GetEventById(Guid id);
        EventResponseDto CreateEvent(EventCreateDto entityDto);
        void UpdateEvent(Guid id, EventCreateDto entityDto);
        void DeleteEvent(Guid id);
    }
}
