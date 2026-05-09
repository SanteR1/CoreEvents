using System.ComponentModel.DataAnnotations;
using CoreEvents.Models.DTOs;

namespace CoreEvents.Models.Domain
{
    public class EventEntity : IEntity
    {
        public Guid Id { get; set; }
        public required string Title { get; set; }
        public string? Description { get; set; }
        public DateTime StartAt { get; set; }
        public DateTime EndAt { get; set; }
        public required int TotalSeats { get; set; }
        private volatile int _availableSeats;
        public int AvailableSeats => _availableSeats;

        private EventEntity() { }

        public static EventEntity Create(string title, string description, DateTime startAt, DateTime endAt, int totalSeats)
        {
            if (totalSeats <= 0)
                throw new ValidationException("Количество мест должно быть больше 0");

            if (endAt <= startAt)
                throw new ArgumentException("Дата окончания не может быть раньше даты начала.");

            return new EventEntity
            {
                Id = Guid.NewGuid(),
                Title = title,
                Description = description,
                StartAt = startAt,
                EndAt = endAt,
                TotalSeats = totalSeats,
                _availableSeats = totalSeats
            };
        }

        public bool TryReserveSeats(int count = 1)
        {
            int current, updated;
            do
            {
                current = _availableSeats;
                if (current < count) return false;

                updated = current - count;
            } while (Interlocked.CompareExchange(ref _availableSeats, updated, current) != current);
            return true;
        }

        public bool ReleaseSeats(int count = 1)
        {
            int current, updated;
            do
            {
                current = _availableSeats;
                if (current < count) return false;

                updated = current + count;
            } while (Interlocked.CompareExchange(ref _availableSeats, updated, current) != current);
            return true;
        }
    }
}
