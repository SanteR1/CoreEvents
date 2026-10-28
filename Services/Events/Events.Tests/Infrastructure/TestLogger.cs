using Microsoft.Extensions.Logging;

namespace Events.Tests.Infrastructure;

public sealed class TestLogger<T> : ILogger<T>
{
    private readonly List<(LogLevel Level, string Message, Exception? Exception)> _logs = new();

    public IReadOnlyList<(LogLevel Level, string Message, Exception? Exception)> Logs => _logs;

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        _logs.Add((logLevel, formatter(state, exception), exception));
    }
}
