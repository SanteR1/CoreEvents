using Events.Application.DTOs;
using Events.Domain.Entities;

namespace Events.Application.Abstractions.Repositories
{
    public interface IEventRepository
    {
        Task<PaginatedResult<Event>> GetAllAsync(EventFilter eventFilter, CancellationToken ct = default);
        Task<Event?> GetByIdAsync(Guid id, CancellationToken ct = default);
        Task<int> SaveChangesAsync(CancellationToken ct = default);
        void Add(Event entity);
        void Update(Event entity);
        void Delete(Event entity);
    }
}
