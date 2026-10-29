namespace CoreEvents.Shared.Contracts.Events;

[EventName("booking.reservation-rejected.v1")]
public record BookingReservationRejected : IIntegrationEvent
{
    public required Guid BookingId { get; init; }
    public required Guid EventId { get; init; }
    public required ValidationFailureReason RejectionReason { get; init; }
}
