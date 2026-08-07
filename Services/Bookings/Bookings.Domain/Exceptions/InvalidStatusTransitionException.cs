using Bookings.Domain.Enums;
using CoreEvents.Shared.Contracts.Exceptions;

namespace Bookings.Domain.Exceptions
{
    public class InvalidStatusTransitionException(BookingStatus currentStatus, BookingStatus newStatus)
    : ConflictException($"Booking with status '{currentStatus}' cannot be modified. Transition to '{newStatus}' is not allowed.")
    {
        public override string ErrorCode => "Booking.InvalidStatusTransition";
        public override object ErrorData => new { currentStatus = currentStatus.ToString(), requestedStatus = newStatus.ToString() };
    }
}
