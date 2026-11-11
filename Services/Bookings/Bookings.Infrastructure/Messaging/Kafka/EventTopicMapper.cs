using Bookings.Application.Abstractions.Messaging;
using CoreEvents.Shared.Contracts.Events;
using CoreEvents.Shared.Contracts.Infrastructure;

namespace Bookings.Infrastructure.Messaging.Kafka;

internal sealed class EventTopicMapper : IEventTopicMapper
{
    private readonly Dictionary<Type, string> _topicMap = new()
    {
        { typeof(BookingReservationRequested), KafkaTopics.BookingTopic },
        { typeof(BookingReservationReleaseRequested), KafkaTopics.BookingTopic },
        { typeof(BookingConfirmed), KafkaTopics.BookingTopic },
        { typeof(BookingRejected), KafkaTopics.BookingTopic },
        { typeof(BookingCancelled), KafkaTopics.BookingTopic }
    };

    public string GetTopicFor<T>()
    {
        if (_topicMap.TryGetValue(typeof(T), out var topic))
            return topic;

        throw new InvalidOperationException($"Topic for event {typeof(T).Name} is not configured.");
    }
}
