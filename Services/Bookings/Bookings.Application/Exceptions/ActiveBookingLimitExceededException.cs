using CoreEvents.Shared.Contracts.Exceptions;

namespace Bookings.Application.Exceptions;

public class ActiveBookingLimitExceededException(int max)
    : ConflictException($"Maximum active bookings per User is '{max}'.")
{
    public override string ErrorCode => "Booking.LimitBooking";
    public override object ErrorData => new { limit = max };
}
