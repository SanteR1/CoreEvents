using CoreEvents.Shared.Contracts.Exceptions;

namespace Bookings.Domain.Exceptions
{
    public class NotBookingOwnerException(Guid bookingId)
        : ForbiddenException($"You do not have permission to booking ID = '{bookingId}'.")
    {
        public override string ErrorCode => "Booking.Denied";
        public override object ErrorData => new { bookingId };
    }
}
