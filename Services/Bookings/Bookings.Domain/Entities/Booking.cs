using Bookings.Domain.Enums;
using Bookings.Domain.Exceptions;
using CoreEvents.Shared.Contracts.Events;

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
    public decimal TotalPrice { get; private set; }
    public decimal UnitPrice { get; private set; }
    public decimal DiscountAmount { get; private set; }
    public string Currency { get; private set; } = "KZT";
    public string? RejectionReason { get; private set; }
    public CancellationReason? CancellationReason { get; private set; }
    public DateTimeOffset? CancellationRequestedAt { get; private set; }

    private Booking() { }

    public static Booking Create(Guid eventId, Guid userId, int seats = 1)
    {
        if (eventId == Guid.Empty)
            throw new ValidationException(nameof(eventId), "Событие не может быть пустым.");

        if (userId == Guid.Empty)
            throw new ValidationException(nameof(userId), "ID пользователя не может быть пустым.");

        if (seats <= 0)
            throw new ValidationException(nameof(seats), "Количество мест должно быть больше 0.");

        return new Booking
        {
            Id = Guid.NewGuid(),
            EventId = eventId,
            Status = BookingStatus.Pending,
            CreatedAt = DateTime.UtcNow,
            UserId = userId,
            Seats = seats,
            Currency = "KZT"
        };
    }

    public void Confirm(decimal unitPrice, decimal totalPrice, string currency, decimal discountAmount = 0m)
    {
        if (unitPrice < 0)
            throw new ValidationException(nameof(unitPrice), "Цена за место не может быть отрицательной.");

        if (totalPrice < 0)
            throw new ValidationException(nameof(totalPrice), "Итоговая цена не может быть отрицательной.");

        if (discountAmount < 0)
            throw new ValidationException(nameof(discountAmount), "Скидка не может быть отрицательной.");

        if (string.IsNullOrWhiteSpace(currency))
            throw new ValidationException(nameof(currency), "Валюта должна быть указана.");

        ChangeStatus(BookingStatus.Confirmed);
        UnitPrice = unitPrice;
        TotalPrice = totalPrice;
        Currency = currency;
        DiscountAmount = discountAmount;
    }

    public void Confirm() => Confirm(UnitPrice, TotalPrice, string.IsNullOrEmpty(Currency) ? "KZT" : Currency, DiscountAmount);

    public void Reject(string reason)
    {
        ChangeStatus(BookingStatus.Rejected);
        RejectionReason = reason;
    }

    public void Reject(ValidationFailureReason reason) => Reject(reason.ToString());

    public void Reject() => Reject("Rejected");

    public void RequestCancellation(CancellationReason reason)
    {
        ChangeStatus(BookingStatus.CancellationPending);
        CancellationReason = reason;
        CancellationRequestedAt = DateTimeOffset.UtcNow;
    }

    public void ApplyCancellation(CancellationReason? reason = null)
    {
        ChangeStatus(BookingStatus.Cancelled);
        if (reason.HasValue)
        {
            CancellationReason = reason.Value;
        }
    }

    public void Cancel() => ApplyCancellation();

    private void ChangeStatus(BookingStatus newStatus)
    {
        var allowed = Status switch
        {
            BookingStatus.Pending =>
                newStatus is BookingStatus.Confirmed
                    or BookingStatus.Rejected
                    or BookingStatus.Cancelled,
            BookingStatus.Confirmed =>
                newStatus is BookingStatus.CancellationPending
                    or BookingStatus.Cancelled,
            BookingStatus.CancellationPending =>
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
