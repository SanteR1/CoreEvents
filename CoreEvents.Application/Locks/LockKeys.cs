namespace CoreEvents.Application.Locks;

internal static class LockKeys
{
    public static string Booking(Guid bookingId) => $"booking:{bookingId}";
    public static string Event(Guid eventId) => $"event:{eventId}";
}
