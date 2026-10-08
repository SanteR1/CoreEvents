using CoreEvents.Shared.Contracts.Exceptions;

namespace Bookings.Application.Exceptions;

public class EventNotFoundException(Guid eventId)
    : NotFoundException($"Event with 'ID' = '{eventId}' was not found.")
{
    public override string ErrorCode => "Event.NotFound";
    public override object ErrorData => new { parameter = "EventId", value = eventId.ToString() };
}
