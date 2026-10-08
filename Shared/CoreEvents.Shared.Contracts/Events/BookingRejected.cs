namespace CoreEvents.Shared.Contracts.Events;

public record BookingRejected
{
    public required Guid BookingId { get; init; }
    public required Guid EventId { get; init; }
    public required Guid UserId { get; init; }
    public required ValidationFailureReason Reason { get; init; }
    public required DateTimeOffset RejectedAt { get; init; } = DateTimeOffset.UtcNow;
}
