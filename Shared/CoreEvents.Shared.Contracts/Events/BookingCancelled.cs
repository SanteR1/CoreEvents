namespace CoreEvents.Shared.Contracts.Events;

[EventName("booking.cancelled.v1")]
public record BookingCancelled : IIntegrationEvent
{
    public required Guid BookingId { get; init; }
    public required Guid EventId { get; init; }
    public required Guid UserId { get; init; }
    public required CancellationReason Reason { get; init; }
    public required DateTimeOffset CancelledAt { get; init; } = DateTimeOffset.UtcNow;
}
