namespace Events.Application.Abstractions.Messaging;

public sealed record InboxEnvelope(
    Guid MessageId,
    Guid CorrelationId,
    Guid? CausationId,
    string ConsumerName,
    string Topic,
    int Partition,
    long Offset,
    string MessageKey,
    string MessageType,
    string Payload,
    string Headers,
    DateTimeOffset ReceivedAt
)
{
    private static long _localOffset;

    public static InboxEnvelope CreateDefault(string messageType) => new(
        MessageId: Guid.NewGuid(),
        CorrelationId: Guid.NewGuid(),
        CausationId: null,
        ConsumerName: "local",
        Topic: "local-topic",
        Partition: 0,
        Offset: Interlocked.Increment(ref _localOffset),
        MessageKey: string.Empty,
        MessageType: messageType,
        Payload: "{}",
        Headers: "{}",
        ReceivedAt: DateTimeOffset.UtcNow);
}
