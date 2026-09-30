namespace CoreEvents.Shared.Contracts.Exceptions;

// Для ошибок аутентификации (HTTP 401)
public abstract class UnauthorizedException(string message, Exception? innerException = null) : AppException(message, innerException);
