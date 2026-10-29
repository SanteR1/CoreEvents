namespace CoreEvents.Shared.Contracts.Events;

[EventName("event.booking-cancellation-completed.v1")]
public record EventBookingCancellationCompleted : IIntegrationEvent
{
    public required Guid BookingId { get; init; }
    public required Guid EventId { get; init; }
    public required bool SeatsReleased { get; init; }
}
