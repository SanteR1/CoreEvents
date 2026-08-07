namespace CoreEvents.Domain.Exceptions;

public class DomainPastEventBookingException(Guid eventId)
    : DomainException($"Event with ID = '{eventId}' has already started or passed.")
{
    public override string ErrorCode => "Event.PastEventBooking";
    public Guid EventId { get; } = eventId;
}
