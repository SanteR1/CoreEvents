using CoreEvents.Models.Domain;
using CoreEvents.Models.DTOs;

namespace CoreEvents.Data.Repositories.Interfaces
{
    internal interface IEventRepository
    {
        Task<PaginatedResult<Event>> GetAllAsync(EventFilter eventFilter, CancellationToken ct = default);
        Task<Event?> GetByIdAsync(Guid id, CancellationToken ct = default);
        Task<int> SaveChangesAsync(CancellationToken ct = default);
        void Add(Event entity);
        void Update(Event entity);
        void Delete(Event entity);
    }
}
