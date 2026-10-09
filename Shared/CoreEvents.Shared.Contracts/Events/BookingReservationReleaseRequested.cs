namespace CoreEvents.Shared.Contracts.Events;

public record BookingReservationReleaseRequested
{
    public required Guid BookingId { get; init; }
    public required Guid EventId { get; init; }
    public required int Seats { get; init; }
    public required CancellationReason Reason { get; init; }
}
