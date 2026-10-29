using CoreEvents.Shared.Contracts.Events;

namespace CoreEvents.Shared.Contracts.Registry;

public interface IIntegrationEventRegistry
{
    string GetName<TEvent>() where TEvent : IIntegrationEvent;
    string GetName(Type eventType);
    Type Resolve(string eventName);
    bool TryResolve(string eventName, out Type? eventType);
}

