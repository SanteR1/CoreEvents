namespace CoreEvents.Shared.Contracts.Exceptions;

public abstract class AppException(string message, Exception? innerException = null)
    : Exception(message, innerException)
{
    public abstract string ErrorCode { get; }

    // Виртуальные свойства для передачи дополнительных данных клиенту
    // По умолчанию дополнительных данных нет, но наследники могут их вернуть
    public virtual object? ErrorData => null;
    public virtual IReadOnlyDictionary<string, string[]>? ValidationErrors => null;
}

// Для ошибок, когда ресурс не найден (HTTP 404)
public abstract class NotFoundException(string message, Exception? innerException = null) : AppException(message, innerException) { }

// Для конфликтов бизнес-логики и состояния (HTTP 409)
public abstract class ConflictException(string message, Exception? innerException = null) : AppException(message, innerException) { }

// Для отказа в доступе из-за отсутствия прав (HTTP 403)
public abstract class ForbiddenException(string message, Exception? innerException = null) : AppException(message, innerException) { }

// Для ошибок аутентификации (HTTP 401)
public abstract class UnauthorizedException(string message, Exception? innerException = null) : AppException(message, innerException) { }

// Для ошибок валидации и неверных данных (HTTP 400)
public abstract class BadRequestException(string message, Exception? innerException = null) : AppException(message, innerException) { }
