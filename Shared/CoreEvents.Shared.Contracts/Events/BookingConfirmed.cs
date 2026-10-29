namespace CoreEvents.Shared.Contracts.Events;

[EventName("booking.confirmed.v1")]
public record BookingConfirmed : IIntegrationEvent
{
    public required Guid BookingId { get; init; }
    public required Guid EventId { get; init; }
    public required Guid UserId { get; init; }
    public required int Seats { get; init; }
}
