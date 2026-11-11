namespace CoreEvents.Shared.Contracts.Exceptions;

public sealed class DataIntegrityException(string message) : NonRetryableException(message);

