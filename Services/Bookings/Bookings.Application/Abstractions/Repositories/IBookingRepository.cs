using Bookings.Application.DTOs;
using Bookings.Domain.Entities;

namespace Bookings.Application.Abstractions.Repositories;

public interface IBookingRepository
{
    Task<Booking?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<int> GetBookingCountForUserAsync(Guid userId, CancellationToken ct = default);
    Task<IReadOnlyList<Guid>> GetPendingAsync(CancellationToken ct = default);
    Task<IReadOnlyList<Booking>> GetActiveBookingsByEventIdAsync(Guid eventId, int limit, CancellationToken ct = default);
    Task<IReadOnlyList<Booking>> GetExpiredPendingBookingsAsync(DateTime threshold, int limit, CancellationToken ct = default);
    Task<IReadOnlyList<Booking>> GetStaleCancellationPendingBookingsAsync(DateTimeOffset threshold, int limit, CancellationToken ct = default);
    Task<PaginatedResult<Booking>> GetUserBookingsAsync(Guid userId, bool isAdmin, BookingFilter filter, CancellationToken ct = default);
    Task<int> SaveChangesAsync(CancellationToken ct = default);
    void Add(Booking booking);
    void Update(Booking booking);
    void Delete(Booking booking);
}
