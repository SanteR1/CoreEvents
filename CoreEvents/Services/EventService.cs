using CoreEvents.Data.Repositories;
using CoreEvents.Models.Domain;
using CoreEvents.Models.DTOs;

namespace CoreEvents.Services
{
    public class EventService : IEventService
    {
        private readonly IRepository<EventEntity> _repository;
        public EventService(IRepository<EventEntity> repository)
        {
            _repository = repository;
        }

        public PaginatedResult GetEvents(EventFilter eventFilter)
        {
            var entity = _repository.GetAll();
            if (!string.IsNullOrWhiteSpace(eventFilter.Title))
            {
                entity = entity.Where(e => e.Title.Contains(eventFilter.Title, StringComparison.OrdinalIgnoreCase));
            }

            if (eventFilter.From is not null)
            {
                entity = entity.Where(e => e.StartAt >= eventFilter.From.Value);
            }

            if (eventFilter.To is not null)
            {
                entity = entity.Where(e => e.EndAt < eventFilter.To.Value.Date.AddDays(1));
            }

            var totalEvents = entity.Count();

            var items = entity
                .OrderByDescending(e => e.StartAt)
                .Skip((eventFilter.Page - 1) * eventFilter.PageSize)
                .Take(eventFilter.PageSize)
                .Select(EventResponseDto.ToDtoCompiled)
                .ToList();

            return new PaginatedResult(
                totalEvents,
                items,
                eventFilter.Page,
                eventFilter.PageSize);
        }

        public EventResponseDto GetEventById(Guid id)
        {
            var entity = _repository.GetById(id);
            if (entity == null) throw new KeyNotFoundException($"Событие с ID {id} не найдено.");

            return new EventResponseDto(
                entity.Id,
                entity.Title,
                entity.Description,
                entity.StartAt,
                entity.EndAt
                );

        }

        public EventResponseDto CreateEvent(EventCreateDto entityDto)
        {
            if (entityDto.EndAt <= entityDto.StartAt)
                throw new ArgumentException("Дата окончания не может быть раньше даты начала.");

            var entity = new EventEntity
            {
                Id = Guid.NewGuid(),
                Title = entityDto.Title,
                Description = entityDto.Description,
                StartAt = entityDto.StartAt,
                EndAt = entityDto.EndAt
            };

            _repository.Add(entity);

            return new EventResponseDto(
                entity.Id,
                entity.Title,
                entity.Description,
                entity.StartAt,
                entity.EndAt
            );
        }
        public void UpdateEvent(Guid id, EventCreateDto entityDto)
        {
            var existing = _repository.GetById(id);
            if (existing == null) throw new KeyNotFoundException("Событие не найдено.");

            if (entityDto.EndAt <= entityDto.StartAt)
                throw new ArgumentException("Дата окончания должна быть позже даты начала.");

            existing.Title = entityDto.Title;
            existing.Description = entityDto.Description;
            existing.StartAt = entityDto.StartAt;
            existing.EndAt = entityDto.EndAt;

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
