namespace CoreEvents.Shared.Contracts.Events;

public record EventBookingCancellationCompleted
{
    public required Guid BookingId { get; init; }
    public required Guid EventId { get; init; }
    public required bool SeatsReleased { get; init; }
}
