namespace CoreEvents.Shared.Contracts.Exceptions;

// Для ошибок валидации и неверных данных (HTTP 400)
public abstract class BadRequestException(string message, Exception? innerException = null) : AppException(message, innerException);
