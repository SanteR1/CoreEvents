namespace CoreEvents.Shared.Contracts.Events;

public static class KafkaTopics
{
    // Публикует Booking-сервис, слушает Event-сервис
    public const string BookingTopic = "booking-topic";
    public const string BookingTopicDlt = $"{BookingTopic}.dlt";
    public const string BookingConfirmed = BookingTopic;
    public const string BookingConfirmedDlt = BookingTopicDlt;

    // Публикует Event-сервис, слушает Booking-сервис
    public const string EventTopic = "event-topic";
    public const string EventTopicDlt = $"{EventTopic}.dlt";
    public const string EventConfirmed = EventTopic;
    public const string EventConfirmedDlt = EventTopicDlt;

    public static readonly IReadOnlyList<string> Booking = [
        BookingTopic,
        BookingTopicDlt
    ];

    public static readonly IReadOnlyList<string> Event = [
        EventConfirmed,
        EventConfirmedDlt
    ];

}
