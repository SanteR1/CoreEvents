namespace CoreEvents.Shared.Contracts.Exceptions;

// Для отказа в доступе из-за отсутствия прав (HTTP 403)
public abstract class ForbiddenException(string message, Exception? innerException = null) : AppException(message, innerException);
