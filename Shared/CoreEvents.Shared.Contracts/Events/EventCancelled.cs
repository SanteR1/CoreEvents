namespace CoreEvents.Shared.Contracts.Events;

[EventName("event.cancelled.v1")]
public record EventCancelled : IIntegrationEvent
{
    public required Guid EventId { get; init; }
    public required string Reason { get; init; }
    public required long Version { get; init; }
    public required DateTimeOffset CancelledAt { get; init; } = DateTimeOffset.UtcNow;
}
