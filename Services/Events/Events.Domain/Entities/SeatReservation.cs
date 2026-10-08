using CoreEvents.Shared.Contracts.Events;
using Events.Domain.Enums;

namespace Events.Domain.Entities;

public sealed class SeatReservation
{
    public Guid BookingId { get; private set; }
    public Guid EventId { get; private set; }
    public int Seats { get; private set; }
    public decimal UnitPrice { get; private set; }
    public decimal TotalPrice { get; private set; }
    public string Currency { get; private set; } = "KZT";
    public decimal DiscountAmount { get; private set; }
    public long PriceVersion { get; private set; }
    public SeatReservationStatus Status { get; private set; }
    public ValidationFailureReason? RejectionReason { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? UpdatedAt { get; private set; }
    public DateTimeOffset? ExpiresAt { get; private set; }
    public Guid CorrelationId { get; private set; }
    public Guid CausationId { get; private set; }
    public uint RowVersion { get; private set; }

    private SeatReservation() { }

    public static SeatReservation CreateReserved(
        Guid bookingId,
        Guid eventId,
        int seats,
        decimal unitPrice,
        string currency,
        decimal discountAmount,
        long priceVersion,
        DateTimeOffset expiresAt,
        Guid correlationId,
        Guid causationId)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(seats);
        ArgumentOutOfRangeException.ThrowIfNegative(unitPrice);
        ArgumentOutOfRangeException.ThrowIfNegative(discountAmount);

        var totalPrice = (seats * unitPrice) - discountAmount;
        if (totalPrice < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(discountAmount), "Discount cannot exceed total base price.");
        }

        if (expiresAt == default)
        {
            throw new ArgumentOutOfRangeException(nameof(expiresAt), "Expiration time must be specified.");
        }

        return new SeatReservation
        {
            BookingId = bookingId,
            EventId = eventId,
            Seats = seats,
            UnitPrice = unitPrice,
            TotalPrice = totalPrice,
            Currency = string.IsNullOrWhiteSpace(currency) ? "KZT" : currency.Trim().ToUpperInvariant(),
            DiscountAmount = discountAmount,
            PriceVersion = priceVersion,
            Status = SeatReservationStatus.Reserved,
            ExpiresAt = expiresAt,
            CreatedAt = DateTimeOffset.UtcNow,
            CorrelationId = correlationId,
            CausationId = causationId
        };
    }

    public static SeatReservation CreateRejected(
        Guid bookingId,
        Guid eventId,
        int seats,
        ValidationFailureReason reason,
        Guid correlationId,
        Guid causationId)
    {
        return new SeatReservation
        {
            BookingId = bookingId,
            EventId = eventId,
            Seats = seats > 0 ? seats : 1,
            UnitPrice = 0.00m,
            TotalPrice = 0.00m,
            Currency = "KZT",
            DiscountAmount = 0.00m,
            PriceVersion = 1,
            Status = SeatReservationStatus.Rejected,
            RejectionReason = reason,
            CreatedAt = DateTimeOffset.UtcNow,
            CorrelationId = correlationId,
            CausationId = causationId
        };
    }

    public static SeatReservation CreateCancelledBeforeReservation(
        Guid bookingId,
        Guid eventId,
        int seats,
        Guid correlationId,
        Guid causationId)
    {
        return new SeatReservation
        {
            BookingId = bookingId,
            EventId = eventId,
            Seats = seats > 0 ? seats : 1,
            UnitPrice = 0.00m,
            TotalPrice = 0.00m,
            Currency = "KZT",
            DiscountAmount = 0.00m,
            PriceVersion = 1,
            Status = SeatReservationStatus.CancelledBeforeReservation,
            CreatedAt = DateTimeOffset.UtcNow,
            CorrelationId = correlationId,
            CausationId = causationId
        };
    }

    public bool Release()
    {
        if (Status != SeatReservationStatus.Reserved)
        {
            return false;
        }

        Status = SeatReservationStatus.Released;
        UpdatedAt = DateTimeOffset.UtcNow;
        return true;
    }
}
