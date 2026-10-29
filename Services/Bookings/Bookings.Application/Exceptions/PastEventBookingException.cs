using CoreEvents.Shared.Contracts.Exceptions;

namespace Bookings.Application.Exceptions;

public class PastEventBookingException(Guid eventId)
    : BadRequestException($"Event with 'ID' = '{eventId}' has already started or passed.")
{
    public override string ErrorCode => "Event.AlreadyPassed";
    public override object ErrorData => new { parameter = "EventId", value = eventId.ToString() };
}
