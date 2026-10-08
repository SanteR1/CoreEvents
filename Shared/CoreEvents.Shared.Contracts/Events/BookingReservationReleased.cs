namespace CoreEvents.Shared.Contracts.Events;

public record BookingReservationReleased
{
    public required Guid BookingId { get; init; }
    public required Guid EventId { get; init; }
    public required int Seats { get; init; }
    public required DateTimeOffset ReleasedAt { get; init; } = DateTimeOffset.UtcNow;
}
