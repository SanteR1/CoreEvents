using CoreEvents.Shared.Contracts.Exceptions;

namespace Events.Application.Exceptions;

public class EventNotFoundException(Guid eventId)
    : NotFoundException($"Event with Id = '{eventId}' was not found.")
{
    public override string ErrorCode => $"Event.NotFound";

    // Передаем детали ошибки прямо в ErrorData для ProblemDetails
    public override object ErrorData => new { parameter = "Id", value = eventId.ToString() };
}
