using CoreEvents.Domain.Enums;

namespace CoreEvents.Domain.Exceptions;

public class DomainInvalidStatusTransitionException(BookingStatus currentStatus, BookingStatus newStatus)
: DomainException($"Booking with status '{currentStatus}' cannot be modified. Transition to '{newStatus}' is not allowed.")
{
    public override string ErrorCode => "Booking.InvalidStatusTransition";

    public BookingStatus CurrentStatus { get; } = currentStatus;
    public BookingStatus NewStatus { get; } = newStatus;
}
