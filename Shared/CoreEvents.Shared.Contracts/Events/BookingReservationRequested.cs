namespace CoreEvents.Shared.Contracts.Events;

[EventName("booking.reservation-requested.v1")]
public record BookingReservationRequested : IIntegrationEvent
{
    public required Guid BookingId { get; init; }
    public required Guid EventId { get; init; }
    public required Guid UserId { get; init; }
    public required int Seats { get; init; }
}
