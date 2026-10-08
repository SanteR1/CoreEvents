using CoreEvents.Shared.Contracts.Exceptions;

namespace Bookings.Application.Exceptions;

public class EventNotActiveException(Guid eventId)
    : BadRequestException($"Event with 'ID' = '{eventId}' is cancelled or inactive.")
{
    public override string ErrorCode => "Event.NotActive";
    public override object ErrorData => new { parameter = "EventId", value = eventId.ToString() };
}
