using CoreEvents.Shared.Contracts.Exceptions;

namespace Bookings.Application.Exceptions
{
    public class BookingNotFoundException(Guid bookingId)
        : NotFoundException($"Booking with 'ID' = '{bookingId}' was not found.")
    {
        public override string ErrorCode => $"Booking.NotFound";
        public override object ErrorData => new { parameter = "Id", value = bookingId.ToString() };
    }
}
