namespace CoreEvents.Shared.Contracts.Events;

public record BookingCancelled
{
    public required Guid BookingId { get; init; }
    public required Guid EventId { get; init; }
    public required Guid UserId { get; init; }
    public required CancellationReason Reason { get; init; }
    public required DateTimeOffset CancelledAt { get; init; } = DateTimeOffset.UtcNow;
}
