namespace Events.Tests.Infrastructure;

using Events.Domain.Entities;
using Events.Domain.Enums;

public static class EventTestExtensions
{
    public static ReserveSeatsResult TryReserveSeats(this Event @event, int count, Guid? bookingId = null, DateTimeOffset? now = null)
    {
        return @event.TryReserveSeats(bookingId ?? Guid.NewGuid(), count, now ?? DateTimeOffset.UtcNow);
    }

    public static void Cancel(this Event @event, DateTimeOffset? now = null)
    {
        @event.Cancel(now ?? DateTimeOffset.UtcNow);
    }
}
