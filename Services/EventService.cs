using CoreEvents.Data.Repositories;
using CoreEvents.Models.Domain;

namespace CoreEvents.Services
{
    public class EventService: IEventService
    {
        private readonly IRepository<EventEntity> _repository;
        public EventService(IRepository<EventEntity> repository)
        {
            _repository = repository;
        }

        public IEnumerable<EventEntity> GetEvents() => _repository.GetAll();

        public EventEntity? GetEventById(Guid id)
        {
            var existing = _repository.GetById(id);
            if (existing == null) throw new KeyNotFoundException("Событие не найдено.");
            return existing;
        }

        public void CreateEvent(EventEntity entity)
        {
            if (entity.EndAt <= entity.StartAt)
                throw new ArgumentException("Дата окончания не может быть раньше даты начала.");

            _repository.Add(entity);
        }
        public void UpdateEvent(Guid id, EventEntity entity)
        {
            var existing = _repository.GetById(id);
            if (existing == null) throw new KeyNotFoundException("Событие не найдено.");

            if (entity.EndAt <= entity.StartAt)
                throw new ArgumentException("Дата окончания должна быть позже даты начала.");

            existing.Title = entity.Title;
            existing.Description = entity.Description;
            existing.StartAt = entity.StartAt;
            existing.EndAt = entity.EndAt;

            _repository.Update(existing);
        }

        public void DeleteEvent(Guid id)
        {
            var existing = _repository.GetById(id);
            if (existing == null) throw new KeyNotFoundException("Событие не найдено.");

            _repository.Delete(id);
        }
    }
}
