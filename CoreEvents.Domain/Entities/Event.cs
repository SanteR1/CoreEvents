using CoreEvents.Domain.Exceptions;

namespace CoreEvents.Domain.Entities;

public sealed class Event
{
    public Guid Id { get; private set; }
    public string Title { get; private set; }
    public string? Description { get; private set; }
    public DateTime StartAt { get; private set; }
    public DateTime EndAt { get; private set; }
    public int TotalSeats { get; private set; }
    public int AvailableSeats { get; private set; }
    public ICollection<Booking> Bookings { get; private set; } = [];

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
        string? description = null)
    {
        Id = id;
        Title = title;
        StartAt = startAt;
        EndAt = endAt;
        TotalSeats = totalSeats;
        AvailableSeats = totalSeats;
        Description = description;
    }

    public static Event Create(
        string? title,
        DateTime? startAt,
        DateTime? endAt,
        int? totalSeats = null,
        string? description = null)
    {
        ThrowIfNotValid(title, startAt, endAt, totalSeats);

        return new Event(
            id: Guid.NewGuid(),
            title: title!.Trim(),
            startAt: startAt!.Value,
            endAt: endAt!.Value,
            totalSeats: totalSeats!.Value,
            description: description);
    }

    public void Update(
        string? title,
        DateTime? startAt,
        DateTime? endAt,
        string? description = null)
    {
        ThrowIfNotValid(title, startAt, endAt, TotalSeats);

        Title = title!;
        StartAt = startAt!.Value;
        EndAt = endAt!.Value;
        Description = description;
    }

    public bool TryReserveSeats(int count = 1)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(count);

        if (AvailableSeats < count) return false;
        AvailableSeats -= count;
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
        return true;
    }

    private static void ThrowIfNotValid(string? title, DateTime? startAt, DateTime? endAt, int? totalSeats)
    {
        var errors = new Dictionary<string, string[]>();

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

        if (errors.Count > 0)
        {
            throw new DomainValidationException(errors);
        }
    }
}
