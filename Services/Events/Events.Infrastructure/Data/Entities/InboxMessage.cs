namespace Events.Infrastructure.Data.Entities;

public sealed class InboxMessage
{
    public Guid Id { get; init; }

    public Guid CorrelationId { get; init; }
    public Guid? CausationId { get; init; }

    public required string ConsumerName { get; init; }
    public required string Topic { get; init; }
    public int Partition { get; init; }
    public long Offset { get; init; }

    public required string MessageKey { get; init; }
    public required string MessageType { get; init; }
    public required string Payload { get; init; }
    public required string Headers { get; init; }

    public DateTimeOffset ReceivedAt { get; init; }
    public DateTimeOffset? ProcessedAt { get; set; }
    public string? LastError { get; set; }

}
