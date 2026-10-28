namespace CoreEvents.Shared.Contracts.Events;

[EventName("booking.reservation-released.v1")]
public record BookingReservationReleased : IIntegrationEvent
{
    public required Guid BookingId { get; init; }
    public required Guid EventId { get; init; }
    public required int Seats { get; init; }
    public required DateTimeOffset ReleasedAt { get; init; } = DateTimeOffset.UtcNow;
}
