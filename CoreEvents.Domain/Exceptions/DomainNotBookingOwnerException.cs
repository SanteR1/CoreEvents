namespace CoreEvents.Domain.Exceptions
{
    public class DomainNotBookingOwnerException(Guid bookingId)
        : DomainException($"You do not have permission to booking ID = '{bookingId}'.")
    {
        public Guid BookingId { get; } = bookingId;
        public override string ErrorCode => $"Access.Denied";
    }
}
