namespace Bookings.Domain.Entities;

public sealed class EventProjection
{
    public Guid Id { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public DateTime StartAt { get; private set; }
    public DateTime EndAt { get; private set; }
    public decimal UnitPrice { get; private set; }
    public string Currency { get; private set; } = "KZT";
    public long PriceVersion { get; private set; } = 1;
    public int TotalSeats { get; private set; }
    public bool IsActive { get; private set; } = true;
    public long Version { get; private set; } = 1;
    public DateTimeOffset UpdatedAt { get; private set; }

    private EventProjection() { }

    public static EventProjection Create(
        Guid id,
        string title,
        DateTime startAt,
        DateTime endAt,
        decimal unitPrice,
        string currency,
        long priceVersion,
        int totalSeats,
        bool isActive = true,
        long version = 1,
        DateTimeOffset? updatedAt = null)
    {
        return new EventProjection
        {
            Id = id,
            Title = title,
            StartAt = startAt,
            EndAt = endAt,
            UnitPrice = unitPrice,
            Currency = string.IsNullOrWhiteSpace(currency) ? "KZT" : currency,
            PriceVersion = priceVersion,
            TotalSeats = totalSeats,
            IsActive = isActive,
            Version = version,
            UpdatedAt = updatedAt ?? DateTimeOffset.UtcNow
        };
    }

    public void Update(
        string title,
        DateTime startAt,
        DateTime endAt,
        decimal unitPrice,
        string currency,
        long priceVersion,
        int totalSeats,
        bool isActive,
        long version,
        DateTimeOffset updatedAt)
    {
        if (version <= Version)
        {
            return;
        }

        Title = title;
        StartAt = startAt;
        EndAt = endAt;
        UnitPrice = unitPrice;
        Currency = string.IsNullOrWhiteSpace(currency) ? "KZT" : currency;
        PriceVersion = priceVersion;
        TotalSeats = totalSeats;
        IsActive = isActive;
        Version = version;
        UpdatedAt = updatedAt;
    }

    public void Cancel(long version, DateTimeOffset cancelledAt)
    {
        if (version < Version)
        {
            return;
        }

        IsActive = false;
        Version = version;
        UpdatedAt = cancelledAt;
    }
}
