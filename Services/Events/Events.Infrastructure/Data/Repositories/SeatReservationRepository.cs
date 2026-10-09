using Events.Application.Abstractions.Repositories;
using Events.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Events.Infrastructure.Data.Repositories;

internal sealed class SeatReservationRepository : ISeatReservationRepository
{
    private readonly EventsDbContext _context;

    public SeatReservationRepository(EventsDbContext context)
    {
        _context = context;
    }

    public async Task<SeatReservation?> GetByBookingIdAsync(Guid bookingId, CancellationToken ct = default)
    {
        return await _context.SeatReservations
            .FirstOrDefaultAsync(r => r.BookingId == bookingId, ct);
    }

    public async Task<IReadOnlyList<SeatReservation>> GetByEventIdAsync(Guid eventId, CancellationToken ct = default)
    {
        return await _context.SeatReservations
            .AsNoTracking()
            .Where(r => r.EventId == eventId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync(ct);
    }

    public void Add(SeatReservation entity)
    {
        _context.SeatReservations.Add(entity);
    }

    public void Update(SeatReservation entity)
    {
        _context.SeatReservations.Update(entity);
    }

    public async Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        return await _context.SaveChangesAsync(ct);
    }
}
