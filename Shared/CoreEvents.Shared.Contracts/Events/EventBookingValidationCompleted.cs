namespace CoreEvents.Shared.Contracts.Events;

public record EventBookingValidationCompleted
{
    public required Guid BookingId { get; init; }
    public required Guid EventId { get; init; }
    public required bool CanBeBooked { get; init; }
    public ValidationFailureReason? FailureReason { get; init; }
}
