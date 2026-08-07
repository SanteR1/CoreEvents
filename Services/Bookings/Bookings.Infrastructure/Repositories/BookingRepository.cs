using Bookings.Application.Abstractions.Repositories;
using Bookings.Domain.Entities;
using Bookings.Domain.Enums;
using Bookings.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Bookings.Infrastructure.Repositories;

internal sealed class BookingRepository : IBookingRepository
{
    private readonly BookingsDbContext _context;

    public BookingRepository(BookingsDbContext context)
    {
        _context = context;
    }
    public async Task<Booking?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _context.Bookings.FindAsync([id], ct);
    }

    public async Task<int> GetBookingCountForUserAsync(Guid userId, CancellationToken ct = default)
    {
        return await _context.Bookings.CountAsync(e => e.UserId == userId && e.Status == BookingStatus.Confirmed, ct);
    }

    public async Task<IReadOnlyList<Guid>> GetPendingAsync(CancellationToken ct = default)
    {
        return await _context.Bookings
            .Where(x => x.Status == BookingStatus.Pending)
            .OrderBy(x => x.CreatedAt)
            .Select(x => x.Id)
            .ToListAsync(ct);
    }

    public async Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        return await _context.SaveChangesAsync(ct);
    }

    public void Add(Booking booking)
    {
        _context.Bookings.Add(booking);
    }

    public void Update(Booking booking)
    {
        _context.Bookings.Update(booking);
    }

    public void Delete(Booking booking)
    {
        _context.Bookings.Remove(booking);
    }
}
