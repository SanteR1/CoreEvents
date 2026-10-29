namespace CoreEvents.Shared.Contracts.Events;

[EventName("booking.created.v1")]
public record BookingCreated : IIntegrationEvent
{
    public required Guid BookingId { get; init; }
    public required Guid EventId { get; init; }
    public required Guid UserId { get; init; }
    public required int Seats { get; init; }
}
