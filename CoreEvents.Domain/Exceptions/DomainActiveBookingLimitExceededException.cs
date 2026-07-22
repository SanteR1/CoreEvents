namespace CoreEvents.Domain.Exceptions
{
    public class DomainActiveBookingLimitExceededException(int max)
        : DomainException($"Maximum active bookings per User is '{max}'.")
    {
        public override string ErrorCode => "Booking.LimitBooking";
        public int Max { get; } = max;
    }
}
