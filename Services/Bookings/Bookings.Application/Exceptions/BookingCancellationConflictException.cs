using CoreEvents.Shared.Contracts.Exceptions;

namespace Bookings.Application.Exceptions;

public class BookingCancellationConflictException(string message) : ConflictException(message)
{
    public override string ErrorCode => "Booking.CancellationConflict";
}
