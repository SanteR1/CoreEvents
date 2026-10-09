using Bookings.Application.Abstractions.Repositories;
using Bookings.Domain.Entities;
using Bookings.Infrastructure.Data;

namespace Bookings.Infrastructure.Repositories;

internal sealed class EventProjectionRepository : IEventProjectionRepository
{
    private readonly BookingsDbContext _context;

    public EventProjectionRepository(BookingsDbContext context)
    {
        _context = context;
    }

    public async Task<EventProjection?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _context.EventProjections.FindAsync([id], ct);
    }

    public async Task AddAsync(EventProjection projection, CancellationToken ct = default)
    {
        await _context.EventProjections.AddAsync(projection, ct);
    }

    public void Update(EventProjection projection)
    {
        _context.EventProjections.Update(projection);
    }

    public async Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        return await _context.SaveChangesAsync(ct);
    }
}
