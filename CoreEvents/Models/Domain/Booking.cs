using System.ComponentModel.DataAnnotations;

namespace CoreEvents.Models.Domain
{
    internal sealed class Booking
    {
        internal Guid Id { get; private set; }
        internal Guid EventId { get; private set; }
        internal BookingStatus Status { get; private set; }
        internal DateTime CreatedAt { get; private set; }
        internal DateTime? ProcessedAt { get; private set; }
        internal Event? Event { get; private set; }
        private Booking() { }

        internal static Booking Create(Guid eventId)
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
        internal void Confirm() => ChangeStatus(BookingStatus.Confirmed);
        internal void Reject() => ChangeStatus(BookingStatus.Rejected);
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
