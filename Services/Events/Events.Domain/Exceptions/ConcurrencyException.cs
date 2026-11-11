using CoreEvents.Shared.Contracts.Exceptions;

namespace Events.Domain.Exceptions;

public class ConcurrencyException : ConflictException
{
    public override string ErrorCode => "Domain.ConcurrencyConflict";

    public ConcurrencyException(string message) : base(message) { }

    public ConcurrencyException(string message, Exception innerException)
        : base(message, innerException) { }
}
