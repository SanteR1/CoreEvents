using Bookings.Domain.Enums;
using Bookings.Domain.Exceptions;

namespace Bookings.Domain.Entities;

public sealed class Booking
{
    public Guid Id { get; private set; }
    public Guid EventId { get; private set; }
    public BookingStatus Status { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? ProcessedAt { get; private set; }
    public Guid UserId { get; private set; }
    public int Seats { get; private set; }
    private Booking() { }

    public static Booking Create(Guid eventId, Guid userId, int seats = 1)
    {
        if (eventId == Guid.Empty)
            throw new ValidationException(nameof(eventId), "Событие не может быть пустым.");

        if (userId == Guid.Empty)
            throw new ValidationException(nameof(userId), "ID пользователя не может быть пустым.");

        if (seats <= 0)
            throw new ValidationException(nameof(seats), "Количество мест должно быть больше 0.");

        return new Booking()
        {
            Id = Guid.NewGuid(),
            EventId = eventId,
            Status = BookingStatus.Pending,
            CreatedAt = DateTime.UtcNow,
            UserId = userId,
            Seats = seats
        };
    }

    public void Cancel() => ChangeStatus(BookingStatus.Cancelled);
    public void Confirm() => ChangeStatus(BookingStatus.Confirmed);
    public void Reject() => ChangeStatus(BookingStatus.Rejected);
    private void ChangeStatus(BookingStatus newStatus)
    {
        var allowed = Status switch
        {
            BookingStatus.Pending =>
                newStatus is BookingStatus.Confirmed
                    or BookingStatus.Rejected
                    or BookingStatus.Cancelled,
            BookingStatus.Confirmed =>
                newStatus is BookingStatus.Cancelled,
            _ => false
        };

        if (!allowed)
        {
            throw new InvalidStatusTransitionException(Status, newStatus);
        }

        Status = newStatus;
        ProcessedAt = DateTime.UtcNow;
    }

    public bool IsOwnedBy(Guid userId) => UserId == userId;

    public void EnsureAccess(Guid userId)
    {
        if (!IsOwnedBy(userId))
        {
            throw new NotBookingOwnerException(Id);
        }
    }
}
