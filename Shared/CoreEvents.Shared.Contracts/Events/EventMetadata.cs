namespace CoreEvents.Shared.Contracts.Events;

public sealed record EventMetadata
{
    public required Guid MessageId { get; init; }
    public required Guid CorrelationId { get; init; } // Для сквозного логирования Саги
    public Guid? CausationId { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
    public required string EventType { get; init; }
}
