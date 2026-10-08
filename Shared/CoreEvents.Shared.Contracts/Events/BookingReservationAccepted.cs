namespace CoreEvents.Shared.Contracts.Events;

public record BookingReservationAccepted
{
    public required Guid BookingId { get; init; }
    public required Guid EventId { get; init; }
    public required int Seats { get; init; }
    public required decimal UnitPrice { get; init; }
    public required decimal TotalPrice { get; init; }
    public required string Currency { get; init; }
    public required decimal DiscountAmount { get; init; }
    public required long PriceVersion { get; init; }
    public DateTimeOffset? ExpiresAt { get; init; }
}
