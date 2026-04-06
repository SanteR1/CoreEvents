using CoreEvents.Models.Domain;

namespace CoreEvents.Services
{
    public interface IEventService
    {
        IEnumerable<EventEntity> GetEvents();
        EventEntity? GetEventById(Guid id);
        void CreateEvent(EventEntity entity);
        void UpdateEvent(Guid id, EventEntity entity);
        void DeleteEvent(Guid id);
    }
}
