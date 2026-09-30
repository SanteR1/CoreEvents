namespace CoreEvents.Shared.Contracts.Exceptions;

// Для ошибок, когда ресурс не найден (HTTP 404)
public abstract class NotFoundException(string message, Exception? innerException = null) : AppException(message, innerException);
