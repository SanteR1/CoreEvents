using CoreEvents.Models.Domain;

namespace CoreEvents.Data.Queues
{
    public interface IBookingQueue
    {
        ValueTask EnqueueAsync(Booking booking, CancellationToken ct);
        IAsyncEnumerable<Booking> DequeueAsync(CancellationToken ct);
    }
}
