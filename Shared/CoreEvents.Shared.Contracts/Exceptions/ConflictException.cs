namespace CoreEvents.Shared.Contracts.Exceptions;

// Для конфликтов бизнес-логики и состояния (HTTP 409)
public abstract class ConflictException(string message, Exception? innerException = null) : AppException(message, innerException);
