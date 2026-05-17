namespace CoreEvents.Models.Domain
{
    public class Booking : IEntity
    {
        public required Guid Id { get; set; }
        public required Guid EventId { get; init; }
        public required BookingStatus Status { get; set; }
        public required DateTime CreatedAt { get; init; }
        public DateTime? ProcessedAt { get; private set; }

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
            ProcessedAt = DateTime.Now;
        }
    }
}
