namespace CoreEvents.Shared.Contracts.Events
{
    public static class KafkaTopics
    {
        // Публикует Booking-сервис, слушает Event-сервис
        public const string BookingConfirmed = "booking-topic";
        public const string BookingConfirmedDlt = $"{BookingConfirmed}.dlt";

        // Публикует Event-сервис, слушает Booking-сервис
        public const string EventConfirmed = "event-topic";
        public const string EventConfirmedDlt = $"{EventConfirmed}.dlt";

        public static readonly IReadOnlyList<string> Booking = [
            BookingConfirmed,
            BookingConfirmedDlt
        ];

        public static readonly IReadOnlyList<string> Event = [
            EventConfirmed,
            EventConfirmedDlt
        ];

    }
}
