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
