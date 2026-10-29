using Bookings.Domain.Entities;

namespace Bookings.Application.Abstractions.Repositories;

public interface IEventProjectionRepository
{
    Task<EventProjection?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task AddAsync(EventProjection projection, CancellationToken ct = default);
    void Update(EventProjection projection);
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
