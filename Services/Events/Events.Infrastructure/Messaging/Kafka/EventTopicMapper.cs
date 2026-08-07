using CoreEvents.Shared.Contracts.Events;
using Events.Application.Abstractions.Messaging;

namespace Events.Infrastructure.Messaging.Kafka;

internal sealed class EventTopicMapper : IEventTopicMapper
{
    private readonly Dictionary<Type, string> _topicMap = new()
    {
        { typeof(EventBookingValidationCompleted), KafkaTopics.EventConfirmed },
        { typeof(EventBookingCancellationCompleted), KafkaTopics.EventConfirmed }

    };

    public string GetTopicFor<T>()
    {
        if (_topicMap.TryGetValue(typeof(T), out var topic))
            return topic;

        throw new InvalidOperationException($"Topic for event {typeof(T).Name} is not configured.");
    }
}
