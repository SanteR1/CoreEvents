using System.Text.Json;
using CoreEvents.Shared.Contracts.Events;
using CoreEvents.Shared.Contracts.Infrastructure;
using CoreEvents.Shared.Contracts.Registry;
using CoreEvents.Shared.Contracts.Serialization;
using Events.Application.Abstractions;
using Events.Application.Abstractions.Caching;
using Events.Application.Abstractions.Messaging;
using Events.Infrastructure.Data.Entities;
using Events.Infrastructure.Messaging.Kafka;

namespace Events.Infrastructure.Data;

internal sealed class OutboxService(
    EventsDbContext dbContext,
    IEventTopicMapper topicMapper,
    IIntegrationEventRegistry registry,
    ICorrelationContext correlationContext)
    : IOutboxService
{
    public const string CacheInvalidationMessageType = "EventCacheInvalidation";
    public const string InternalCacheTopic = "internal:cache";

    public void AddIntegrationEvent<TEvent>(TEvent integrationEvent, string partitionKey)
        where TEvent : IIntegrationEvent
    {
        var eventType = registry.GetName<TEvent>();
        var metadata = new EventMetadata
        {
            MessageId = Guid.CreateVersion7(),
            CorrelationId = correlationContext.CorrelationId != Guid.Empty ? correlationContext.CorrelationId : Guid.NewGuid(),
            CausationId = correlationContext.CausationId,
            CreatedAt = DateTimeOffset.UtcNow,
            EventType = eventType
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
            Topic = topicMapper.GetTopicFor<TEvent>()
        };

        dbContext.OutboxMessages.Add(outboxMessage);
    }

    public void AddCacheInvalidation(Guid eventId)
    {
        var key = CacheKeys.Event(eventId);

        // Дедупликация в рамках текущего UoW: проверяем ChangeTracker
        var alreadyQueued = dbContext.ChangeTracker.Entries<OutboxMessage>()
            .Any(e => string.Equals(e.Entity.MessageType, CacheInvalidationMessageType, StringComparison.Ordinal) &&
                      string.Equals(e.Entity.Key, key, StringComparison.Ordinal));

        if (alreadyQueued)
            return;

        var metadata = new EventMetadata
        {
            MessageId = Guid.CreateVersion7(),
            CorrelationId = correlationContext.CorrelationId != Guid.Empty ? correlationContext.CorrelationId : Guid.NewGuid(),
            CausationId = correlationContext.CausationId,
            CreatedAt = DateTimeOffset.UtcNow,
            EventType = CacheInvalidationMessageType
        };

        var headers = EventMetadataHeaderMapper.ToHeaderDictionary(metadata);

        var outboxMessage = new OutboxMessage
        {
            Id = metadata.MessageId,
            Key = key,
            MessageType = CacheInvalidationMessageType,
            Payload = JsonSerializer.Serialize(new { EventId = eventId }),
            Headers = JsonSerializer.Serialize(headers, IntegrationEventJsonOptions.Default),
            CorrelationId = metadata.CorrelationId,
            CausationId = metadata.CausationId,
            CreatedAt = metadata.CreatedAt,
            Topic = InternalCacheTopic
        };

        dbContext.OutboxMessages.Add(outboxMessage);
    }
}
