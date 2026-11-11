namespace CoreEvents.Shared.Contracts.Events;

[EventName("booking.reservation-release-requested.v1")]
public record BookingReservationReleaseRequested : IIntegrationEvent
{
    public required Guid BookingId { get; init; }
    public required Guid EventId { get; init; }
    public required int Seats { get; init; }
    public required CancellationReason Reason { get; init; }
}
