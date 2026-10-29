namespace CoreEvents.Shared.Contracts.Exceptions;

public sealed class UnknownEventTypeException(string eventType)
    : NonRetryableException($"Неизвестный тип события: {eventType}")
{
    public string EventType { get; } = eventType;
}

