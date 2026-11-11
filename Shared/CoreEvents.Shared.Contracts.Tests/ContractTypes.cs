namespace CoreEvents.Shared.Contracts.Tests;

using CoreEvents.Shared.Contracts.Events;

public static class ContractTypes
{
    public static IReadOnlyList<Type> AllConcreteIntegrationEvents() =>
        typeof(IIntegrationEvent).Assembly.GetTypes()
            .Where(t => t.Namespace is { } ns
                     && (string.Equals(ns, "CoreEvents.Shared.Contracts.Events", StringComparison.Ordinal)
                         || ns.StartsWith("CoreEvents.Shared.Contracts.Events.", StringComparison.Ordinal))
                     && t is { IsClass: true, IsAbstract: false, IsNested: false, IsPublic: true }
                     && !typeof(Attribute).IsAssignableFrom(t))
            .ToList();
}

