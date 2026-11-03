using System.Collections.Concurrent;
using System.Globalization;
using Serilog.Core;
using Serilog.Events;

namespace Events.IntegrationTests.Infrastructure.Logging;

public sealed record ConcurrencyRetryLogEntry
{
    public required string Message { get; init; }
    public required string? EventId { get; init; }
    public required DateTimeOffset Timestamp { get; init; }
}

public sealed class TestConcurrencyLogCollector : ILogEventSink
{
    private readonly ConcurrentBag<ConcurrencyRetryLogEntry> _entries = new();

    public IReadOnlyCollection<ConcurrencyRetryLogEntry> Entries => _entries;

    public void Emit(LogEvent logEvent)
    {
        if (logEvent.MessageTemplate.Text.Contains("[Concurrency] Конфликт версий данных", StringComparison.OrdinalIgnoreCase))
        {
            string? eventId = null;
            if (logEvent.Properties.TryGetValue("DomainEventId", out var domainProp))
            {
                eventId = domainProp is ScalarValue scalar && scalar.Value != null
                    ? Convert.ToString(scalar.Value, CultureInfo.InvariantCulture)
                    : Convert.ToString(domainProp, CultureInfo.InvariantCulture);
            }
            else if (logEvent.Properties.TryGetValue("EventId", out var prop))
            {
                // Исключаем системный EventId от Microsoft.Extensions.Logging (он сериализуется как StructureValue с полями Id и Name)
                if (prop is ScalarValue scalar && scalar.Value != null)
                {
                    var str = Convert.ToString(scalar.Value, CultureInfo.InvariantCulture);
                    if (Guid.TryParse(str, out _))
                    {
                        eventId = str;
                    }
                }
            }
            eventId = eventId?.Trim('\"');

            _entries.Add(new ConcurrencyRetryLogEntry
            {
                Message = logEvent.RenderMessage(CultureInfo.InvariantCulture),
                EventId = eventId,
                Timestamp = DateTimeOffset.UtcNow
            });
        }
    }
}
