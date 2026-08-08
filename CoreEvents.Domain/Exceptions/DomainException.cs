namespace CoreEvents.Domain.Exceptions;

public abstract class DomainException(string message, Exception? innerException = null)
    : Exception(message, innerException)
{
    public abstract string ErrorCode { get; }

}
