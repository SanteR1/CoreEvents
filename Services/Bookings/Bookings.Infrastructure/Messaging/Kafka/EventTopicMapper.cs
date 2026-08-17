using Bookings.Application.Abstractions.Messaging;
using CoreEvents.Shared.Contracts.Events;

namespace Bookings.Infrastructure.Messaging.Kafka;

internal sealed class EventTopicMapper : IEventTopicMapper
{
    private readonly Dictionary<Type, string> _topicMap = new()
    {
        { typeof(BookingCancellationRequested), KafkaTopics.BookingConfirmed },
        { typeof(BookingCreated), KafkaTopics.BookingConfirmed },
        { typeof(BookingConfirmed), KafkaTopics.BookingConfirmed }
    };

    public string GetTopicFor<T>()
    {
        if (_topicMap.TryGetValue(typeof(T), out var topic))
            return topic;

        throw new InvalidOperationException($"Topic for event {typeof(T).Name} is not configured.");
    }
}
