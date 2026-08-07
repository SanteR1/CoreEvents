using CoreEvents.Domain.Enums;
using CoreEvents.Domain.Exceptions;

namespace CoreEvents.Domain.Entities;

public sealed class Booking
{
    public Guid Id { get; private set; }
    public Guid EventId { get; private set; }
    public BookingStatus Status { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? ProcessedAt { get; private set; }
    public Event? Event { get; private set; }
    public User? User { get; private set; }
    public Guid UserId { get; private set; }
    private Booking() { }

    public static Booking Create(Guid eventId, Guid userId)
    {
        if (eventId == Guid.Empty)
            throw new DomainValidationException(nameof(eventId), "Событие не может быть пустым.");

        if (userId == Guid.Empty)
            throw new DomainValidationException(nameof(userId), "ID пользователя не может быть пустым.");

        return new Booking()
        {
            Id = Guid.NewGuid(),
            EventId = eventId,
            Status = BookingStatus.Pending,
            CreatedAt = DateTime.UtcNow,
            UserId = userId
        };
    }
    public void Confirm() => ChangeStatus(BookingStatus.Confirmed);
    public void Reject() => ChangeStatus(BookingStatus.Rejected);
    public void Cancelled() => ChangeStatus(BookingStatus.Cancelled);
    private void ChangeStatus(BookingStatus newStatus)
    {
        var allowed = Status switch
        {
            BookingStatus.Pending =>
                newStatus is BookingStatus.Confirmed
                    or BookingStatus.Rejected
            or BookingStatus.Cancelled,
            BookingStatus.Confirmed =>
                newStatus is BookingStatus.Rejected
                    or BookingStatus.Cancelled,
            BookingStatus.Rejected =>
                newStatus is BookingStatus.Rejected
                    or BookingStatus.Cancelled,
            _ => false
        };

        if (!allowed)
        {
            throw new DomainInvalidStatusTransitionException(Status, newStatus);
        }

        Status = newStatus;
        ProcessedAt = DateTime.UtcNow;
    }
}
