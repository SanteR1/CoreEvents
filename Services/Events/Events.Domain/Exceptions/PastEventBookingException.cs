using CoreEvents.Shared.Contracts.Exceptions;

namespace Events.Domain.Exceptions;

public class PastEventBookingException(Guid eventId)
    : BadRequestException($"Event with ID = '{eventId}' has already started or passed.")
{
    public override string ErrorCode => "Event.PastEventBooking";
    public override object ErrorData => new { eventId };
}
