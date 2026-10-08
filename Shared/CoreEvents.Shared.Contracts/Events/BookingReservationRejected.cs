namespace CoreEvents.Shared.Contracts.Events;

public record BookingReservationRejected
{
    public required Guid BookingId { get; init; }
    public required Guid EventId { get; init; }
    public required ValidationFailureReason RejectionReason { get; init; }
}
