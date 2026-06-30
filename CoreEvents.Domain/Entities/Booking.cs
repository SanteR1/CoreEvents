using System.ComponentModel.DataAnnotations;
using CoreEvents.Domain.Enums;

namespace CoreEvents.Domain.Entities
{
    public sealed class Booking
    {
        public Guid Id { get; private set; }
        public Guid EventId { get; private set; }
        public BookingStatus Status { get; private set; }
        public DateTime CreatedAt { get; private set; }
        public DateTime? ProcessedAt { get; private set; }
        public Event? Event { get; private set; }
        private Booking() { }

        public static Booking Create(Guid eventId)
        {
            if (eventId == Guid.Empty)
                throw new ValidationException(
                    new ValidationResult("Событие не может быть пустым.", [nameof(eventId)]),
                    validatingAttribute: null,
                    value: eventId);

            return new Booking()
            {
                Id = Guid.NewGuid(),
                EventId = eventId,
                Status = BookingStatus.Pending,
                CreatedAt = DateTime.UtcNow
            };
        }
        public void Confirm() => ChangeStatus(BookingStatus.Confirmed);
        public void Reject() => ChangeStatus(BookingStatus.Rejected);
        private void ChangeStatus(BookingStatus newStatus)
        {
            var allowed = Status switch
            {
                BookingStatus.Pending =>
                    newStatus is BookingStatus.Confirmed
                        or BookingStatus.Rejected,
                BookingStatus.Confirmed =>
                    newStatus is BookingStatus.Rejected,
                BookingStatus.Rejected =>
                    newStatus is BookingStatus.Rejected,
                _ => false
            };

            if (!allowed)
            {
                throw new InvalidOperationException(
                    $"Бронь со статусом {Status} не может быть изменена. Переход {Status} -> {newStatus} запрещён.");
            }

            Status = newStatus;
            ProcessedAt = DateTime.UtcNow;
        }
    }
}
