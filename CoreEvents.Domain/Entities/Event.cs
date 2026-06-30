using System.ComponentModel.DataAnnotations;

namespace CoreEvents.Domain.Entities
{
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
            if (string.IsNullOrWhiteSpace(title))
                throw new ValidationException(
                    new ValidationResult("Название не может быть пустым.", [nameof(title)]),
                    validatingAttribute: null,
                    value: title);

            if (!startAt.HasValue)
                throw new ValidationException(
                    new ValidationResult("Дата начала не может быть пустым.", [nameof(startAt)]),
                    validatingAttribute: null,
                    value: startAt);

            if (!endAt.HasValue)
                throw new ValidationException(
                    new ValidationResult("Дата окончания не может быть пустым.", [nameof(endAt)]),
                    validatingAttribute: null,
                    value: endAt);

            if (startAt <= DateTime.UtcNow.AddMilliseconds(-100))
                throw new ValidationException(
                    new ValidationResult("Событие не может начинаться в прошлом.", [nameof(startAt)]),
                    validatingAttribute: null,
                    value: startAt);

            if (endAt < startAt)
                throw new ValidationException(
                    new ValidationResult("Дата окончания не может быть раньше даты начала.", [nameof(endAt)]),
                    validatingAttribute: null,
                    value: endAt);

            if (endAt == startAt)
                throw new ValidationException(
                    new ValidationResult("Дата начала и Дата окончания не могут быть одинаковыми.", [nameof(startAt), nameof(endAt)]),
                    validatingAttribute: null,
                    value: new { startAt, endAt });

            if (!totalSeats.HasValue || totalSeats.Value <= 0)
                throw new ValidationException(
                    new ValidationResult("Количество мест должно быть больше 0.", [nameof(totalSeats)]),
                    validatingAttribute: null,
                    value: totalSeats);
        }
    }
}
