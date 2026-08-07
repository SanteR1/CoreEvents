using System.Text.Json;
using Bookings.Application.Abstractions;
using Bookings.Application.Abstractions.Messaging;
using Bookings.Infrastructure.Data.Entities;
using Bookings.Infrastructure.Messaging.Kafka;
using CoreEvents.Shared.Contracts.Events;
using CoreEvents.Shared.Contracts.Serialization;

namespace Bookings.Infrastructure.Data
{
    internal sealed class OutboxService(BookingsDbContext dbContext, IEventTopicMapper topicMapper, ICorrelationContext correlationContext) : IOutboxService
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
}
