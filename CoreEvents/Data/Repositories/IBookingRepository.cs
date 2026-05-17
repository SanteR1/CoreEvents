using CoreEvents.Models.Domain;

namespace CoreEvents.Data.Repositories
{
    public interface IBookingRepository : IRepository<Booking>
    {
        IEnumerable<Booking> GetPending(CancellationToken ct = default);
    }
}
