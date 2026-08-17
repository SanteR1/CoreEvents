using System.Text.Json;
using CoreEvents.Shared.Contracts.Events;
using CoreEvents.Shared.Contracts.Serialization;
using Events.Application.Abstractions;
using Events.Application.Abstractions.Messaging;
using Events.Infrastructure.Data.Entities;
using Events.Infrastructure.Messaging.Kafka;

namespace Events.Infrastructure.Data;

internal sealed class OutboxService(EventsDbContext dbContext, IEventTopicMapper topicMapper, ICorrelationContext correlationContext) : IOutboxService
{
    public void Publish<T>(T integrationEvent, string partitionKey)
    {
        var metadata = new EventMetadata
        {
            MessageId = Guid.CreateVersion7(),
            CorrelationId = correlationContext.CorrelationId,
            CausationId = correlationContext.CausationId,
            CreatedAt = DateTimeOffset.UtcNow,
            EventType = typeof(T).Name
        };

        var headers = EventMetadataHeaderMapper.ToHeaderDictionary(metadata);

        var outboxMessage = new OutboxMessage
        {
            Id = metadata.MessageId,
            Key = partitionKey,
            MessageType = metadata.EventType,
            Payload = JsonSerializer.Serialize(integrationEvent, IntegrationEventJsonOptions.Default),
            Headers = JsonSerializer.Serialize(headers, IntegrationEventJsonOptions.Default),
            CorrelationId = metadata.CorrelationId,
            CausationId = metadata.CausationId,
            CreatedAt = metadata.CreatedAt,
            Topic = topicMapper.GetTopicFor<T>()
        };

        dbContext.OutboxMessages.Add(outboxMessage);
    }
}
