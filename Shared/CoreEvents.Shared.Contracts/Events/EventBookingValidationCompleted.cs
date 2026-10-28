namespace CoreEvents.Shared.Contracts.Events;

[EventName("event.booking-validation-completed.v1")]
public record EventBookingValidationCompleted : IIntegrationEvent
{
    public required Guid BookingId { get; init; }
    public required Guid EventId { get; init; }
    public required bool CanBeBooked { get; init; }
    public ValidationFailureReason? FailureReason { get; init; }
}
