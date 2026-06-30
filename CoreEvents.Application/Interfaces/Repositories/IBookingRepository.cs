using CoreEvents.Domain.Entities;

namespace CoreEvents.Application.Interfaces.Repositories
{
    public interface IBookingRepository
    {
        Task<Booking?> GetByIdAsync(Guid id, CancellationToken ct = default);
        Task<int> SaveChangesAsync(CancellationToken ct = default);
        Task<IReadOnlyList<Guid>> GetPendingAsync(CancellationToken ct = default);
        void Add(Booking booking);
        void Update(Booking booking);
        void Delete(Booking booking);
    }
}
