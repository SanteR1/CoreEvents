namespace CoreEvents.Shared.Contracts.Events;

[EventName("booking.cancellation-requested.v1")]
public record BookingCancellationRequested : IIntegrationEvent
{
    public required Guid BookingId { get; init; }
    public required Guid EventId { get; init; }
    public required Guid UserId { get; init; }
    public required int Seats { get; init; }

    /// <summary>
    /// Причина отмены (например: "Seats not available", "User cancelled", "Timeout")
    /// </summary>
    public required CancellationReason Reason { get; init; }

    /// <summary>
    /// Момент времени, когда бронь была отменена
    /// </summary>
    public required DateTimeOffset CancelledAt { get; init; } = DateTimeOffset.UtcNow;
}
