namespace CoreEvents.Shared.Contracts.Exceptions;

public abstract class NonRetryableException(string message, Exception? inner = null)
    : Exception(message, inner);

