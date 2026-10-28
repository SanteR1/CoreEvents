namespace Events.Application.Abstractions.Messaging;

public interface IIntegrationEventDispatcher
{
    IReadOnlyCollection<string> SupportedEventTypes { get; }
    Task DispatchAsync(string eventType, string payload, InboxEnvelope env, CancellationToken ct);
}
