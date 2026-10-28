using Events.Application.Abstractions.Messaging;
using Events.Application.Abstractions.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Events.Infrastructure.Data.Repositories;

internal sealed class InboxRepository(EventsDbContext dbContext) : IInboxRepository
{
    public async Task<bool> TryRegisterAsync(InboxEnvelope envelope, CancellationToken ct)
    {
        var now = DateTimeOffset.UtcNow;
        var affected = await dbContext.Database
            .ExecuteSqlInterpolatedAsync($"""
                INSERT INTO inbox_messages (
                    id, consumer_name, correlation_id, causation_id, topic, partition, "offset",
                    message_key, message_type, payload, headers, received_at, processed_at)
                VALUES (
                    {envelope.MessageId}, {envelope.ConsumerName}, {envelope.CorrelationId}, {envelope.CausationId},
                    {envelope.Topic}, {envelope.Partition}, {envelope.Offset}, {envelope.MessageKey}, {envelope.MessageType},
                    {envelope.Payload}::jsonb, {envelope.Headers}::jsonb, {envelope.ReceivedAt}, {now})
                ON CONFLICT
                DO NOTHING
                """, ct);

        return affected == 1;
    }
}
