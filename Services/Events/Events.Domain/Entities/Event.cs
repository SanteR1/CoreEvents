using Events.Domain.DomainEvents;
using Events.Domain.Exceptions;

namespace Events.Domain.Entities;

public sealed class Event
{
    public Guid Id { get; private set; }
    public string Title { get; private set; }
    public string? Description { get; private set; }
    public DateTime StartAt { get; private set; }
    public DateTime EndAt { get; private set; }
    public int TotalSeats { get; private set; }
    public int AvailableSeats { get; private set; }
    public decimal Price { get; private set; } = 0.00m;
    public string Currency { get; private set; } = "KZT";
    public long PriceVersion { get; private set; } = 1;
    public bool IsActive { get; private set; } = true;
    public long Version { get; private set; } = 1;
    public uint RowVersion { get; private set; }

    // Приватный конструктор, чтобы никто не создал объект в обход метода Create
    private Event()
    {
        Title = null!;
    }
    private Event(
        Guid id,
        string title,
        DateTime startAt,
        DateTime endAt,
        int totalSeats,
        string? description = null,
        decimal price = 0.00m,
        string currency = "KZT")
    {
        Id = id;
        Title = title;
        StartAt = startAt;
        EndAt = endAt;
        TotalSeats = totalSeats;
        AvailableSeats = totalSeats;
        Description = description;
        Price = price;
        Currency = string.IsNullOrWhiteSpace(currency) ? "KZT" : currency.Trim().ToUpperInvariant();
        PriceVersion = 1;
        IsActive = true;
        Version = 1;
    }

    public static Event Create(
        string? title,
        DateTime? startAt,
        DateTime? endAt,
        int? totalSeats = null,
        string? description = null,
        decimal price = 0.00m,
        string currency = "KZT")
    {
        ThrowIfNotValid(title, startAt, endAt, totalSeats, price);

        return new Event(
            id: Guid.NewGuid(),
            title: title!.Trim(),
            startAt: startAt!.Value,
            endAt: endAt!.Value,
            totalSeats: totalSeats!.Value,
            description: description,
            price: price,
            currency: currency);
    }

    public void Update(
        string? title,
        DateTime? startAt,
        DateTime? endAt,
        string? description = null,
        decimal? price = null,
        string? currency = null)
    {
        var targetPrice = price ?? Price;
        ThrowIfNotValid(title, startAt, endAt, TotalSeats, targetPrice);

        Title = title!;
        StartAt = startAt!.Value;
        EndAt = endAt!.Value;
        Description = description;

        var priceOrCurrencyChanged = (price.HasValue && price.Value != Price)
            || (!string.IsNullOrWhiteSpace(currency) && !string.Equals(currency.Trim(), Currency, StringComparison.OrdinalIgnoreCase));

        if (priceOrCurrencyChanged)
        {
            Price = targetPrice;
            if (!string.IsNullOrWhiteSpace(currency))
            {
                Currency = currency.Trim().ToUpperInvariant();
            }
            PriceVersion++;
        }

        Version++;
    }

    public void Cancel()
    {
        if (!IsActive) return;
        IsActive = false;
        Version++;
    }

    public bool TryReserveSeats(int count = 1)
    {
        if (!IsActive) return false;
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(count);

        if (AvailableSeats < count) return false;
        AvailableSeats -= count;

        RaiseDomainEvent(new SeatsReserved(Id));

        return true;
    }

    public bool ReleaseSeats(int count = 1)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(count);

        if (AvailableSeats + count > TotalSeats)
        {
            return false;
        }

        AvailableSeats += count;

        RaiseDomainEvent(new SeatsReleased(Id));

        return true;
    }

    private static void ThrowIfNotValid(string? title, DateTime? startAt, DateTime? endAt, int? totalSeats, decimal? price = null)
    {
        var errors = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);

        void AddError(string key, string message)
        {
            errors[key] = errors.TryGetValue(key, out var existing)
                ? [.. existing, message]
                : [message];
        }

        if (string.IsNullOrWhiteSpace(title))
            AddError(nameof(title), "Название не может быть пустым.");

        if (!startAt.HasValue)
            AddError(nameof(startAt), "Дата начала не может быть пустой.");
        else if (startAt <= DateTime.UtcNow.AddMilliseconds(-100))
            AddError(nameof(startAt), "Событие не может начинаться в прошлом.");

        if (!endAt.HasValue)
            AddError(nameof(endAt), "Дата окончания не может быть пустой.");
        else if (startAt.HasValue && endAt < startAt)
            AddError(nameof(endAt), "Дата окончания не может быть раньше даты начала.");

        if (startAt.HasValue && endAt.HasValue && endAt == startAt)
        {
            const string equalityMsg = "Дата начала и дата окончания не могут быть одинаковыми.";
            AddError(nameof(startAt), equalityMsg);
            AddError(nameof(endAt), equalityMsg);
        }

        if (!totalSeats.HasValue || totalSeats.Value <= 0)
            AddError(nameof(totalSeats), "Количество мест должно быть больше 0.");

        if (price.HasValue && price.Value < 0)
            AddError("price", "Цена не может быть отрицательной.");

        if (errors.Count > 0)
        {
            throw new ValidationException(errors);
        }
    }

    private readonly List<IDomainEvent> _domainEvents = new();
    private void RaiseDomainEvent(IDomainEvent domainEvent) => _domainEvents.Add(domainEvent);
    public IReadOnlyList<IDomainEvent> PopDomainEvents()
    {
        var events = _domainEvents.ToList();
        _domainEvents.Clear();
        return events;
    }
}
