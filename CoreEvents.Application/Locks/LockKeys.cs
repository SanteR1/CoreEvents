namespace CoreEvents.Application.Locks
{
    internal static class LockKeys
    {
        public static string BookingProcessing(Guid bookingId) => $"bookingorchestrator:{bookingId}";
        public static string Event(Guid eventId) => $"event:{eventId}";
    }
}
