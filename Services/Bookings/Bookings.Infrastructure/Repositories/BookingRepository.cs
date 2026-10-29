using Bookings.Application.Abstractions.Repositories;
using Bookings.Application.DTOs;
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
        return await _context.Bookings.CountAsync(
            e => e.UserId == userId && (e.Status == BookingStatus.Confirmed || e.Status == BookingStatus.Pending),
            ct);
    }

    public async Task<IReadOnlyList<Guid>> GetPendingAsync(CancellationToken ct = default)
    {
        return await _context.Bookings
            .Where(x => x.Status == BookingStatus.Pending)
            .OrderBy(x => x.CreatedAt)
            .Select(x => x.Id)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<Booking>> GetActiveBookingsByEventIdAsync(Guid eventId, int limit, CancellationToken ct = default)
    {
        return await _context.Bookings
            .Where(b => b.EventId == eventId && (b.Status == BookingStatus.Confirmed || b.Status == BookingStatus.CancellationPending))
            .OrderBy(b => b.CreatedAt)
            .Take(limit)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<Booking>> GetExpiredPendingBookingsAsync(DateTime threshold, int limit, CancellationToken ct = default)
    {
        return await _context.Bookings
            .Where(b => b.Status == BookingStatus.Pending && b.CreatedAt <= threshold)
            .OrderBy(b => b.CreatedAt)
            .Take(limit)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<Booking>> GetStaleCancellationPendingBookingsAsync(DateTimeOffset threshold, int limit, CancellationToken ct = default)
    {
        return await _context.Bookings
            .Where(b => b.Status == BookingStatus.CancellationPending &&
                        b.CancellationRequestedAt.HasValue &&
                        b.CancellationRequestedAt.Value <= threshold)
            .OrderBy(b => b.CancellationRequestedAt)
            .Take(limit)
            .ToListAsync(ct);
    }

    public async Task<PaginatedResult<Booking>> GetUserBookingsAsync(
        Guid userId,
        bool isAdmin,
        BookingFilter filter,
        CancellationToken ct = default)
    {
        IQueryable<Booking> query = _context.Bookings.AsNoTracking();

        if (!isAdmin)
        {
            query = query.Where(b => b.UserId == userId);
        }
        else if (filter.UserId.HasValue)
        {
            query = query.Where(b => b.UserId == filter.UserId.Value);
        }

        if (filter.Status.HasValue)
        {
            query = query.Where(b => b.Status == filter.Status.Value);
        }

        var totalCount = await query.CountAsync(ct);

        int page = filter.Page > 0 ? filter.Page : 1;
        int pageSize = filter.PageSize > 0 ? Math.Min(filter.PageSize, 100) : 10;

        var items = await query
            .OrderByDescending(b => b.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return new PaginatedResult<Booking>(items, totalCount, page, pageSize);
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
