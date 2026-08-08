namespace CoreEvents.Domain.Exceptions;

public class DomainReleaseSeatsException(Guid eventId, Guid bookingId)
    : DomainException($"Event with ID = '{eventId}' failed to release seats with Booking ID = '{bookingId}'.")
{
    public override string ErrorCode => "Event.ReleaseEventBooking";
    public Guid EventId { get; } = eventId;
    public Guid BookingId { get; } = bookingId;
}
