using CoreEvents.Application.Interfaces.Repositories;
using CoreEvents.Domain.Entities;
using CoreEvents.Domain.Enums;
using CoreEvents.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CoreEvents.Infrastructure.Repositories
{
    internal sealed class BookingRepository: IBookingRepository
    {
        private readonly AppDbContext _context;

        public BookingRepository(AppDbContext context)
        {
            _context = context;
        }
        public async Task<Booking?> GetByIdAsync(Guid id, CancellationToken ct = default)
        {
            return await _context.Bookings.FindAsync([id], ct);
        }

        public async Task<int> SaveChangesAsync(CancellationToken ct = default)
        {
            return await _context.SaveChangesAsync(ct);
        }

        public async Task<IReadOnlyList<Guid>> GetPendingAsync(CancellationToken ct = default)
        {
            return await _context.Bookings
                .Where(x => x.Status == BookingStatus.Pending)
                .OrderBy(x=> x.CreatedAt)
                .Select(x => x.Id)
                .ToListAsync(ct);
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
}
