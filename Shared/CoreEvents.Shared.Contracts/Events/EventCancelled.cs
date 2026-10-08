namespace CoreEvents.Shared.Contracts.Events;

public record EventCancelled
{
    public required Guid EventId { get; init; }
    public required string Reason { get; init; }
    public required long Version { get; init; }
    public required DateTimeOffset CancelledAt { get; init; } = DateTimeOffset.UtcNow;
}
