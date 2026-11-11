using System.Collections.Frozen;
using System.Reflection;
using System.Text.RegularExpressions;
using CoreEvents.Shared.Contracts.Events;

namespace CoreEvents.Shared.Contracts.Registry;

public sealed class IntegrationEventRegistry : IIntegrationEventRegistry
{
    private static readonly Regex WireNameVersionRegex = new(
        @"^[a-z0-9]+(?:-[a-z0-9]+)*\.[a-z0-9]+(?:-[a-z0-9]+)*(?:\.[a-z0-9]+(?:-[a-z0-9]+)*)*\.v[1-9][0-9]*$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant,
        TimeSpan.FromSeconds(1));

    private readonly FrozenDictionary<Type, string> _typeToName;
    private readonly FrozenDictionary<string, Type> _nameToType;

    public IntegrationEventRegistry(IEnumerable<Assembly>? assemblies = null)
    {
        var targetAssemblies = assemblies?.Where(a => a != null).Distinct().ToArray() is { Length: > 0 } arr
            ? arr
            : [typeof(IIntegrationEvent).Assembly];

        var typeToName = new Dictionary<Type, string>();
        var nameToType = new Dictionary<string, Type>(StringComparer.Ordinal);

        foreach (var assembly in targetAssemblies)
        {
            var eventTypes = assembly.GetTypes()
                .Where(t => typeof(IIntegrationEvent).IsAssignableFrom(t)
                         && t is { IsClass: true, IsAbstract: false, ContainsGenericParameters: false });

            foreach (var type in eventTypes)
            {
                var attr = type.GetCustomAttribute<EventNameAttribute>(inherit: false)
                    ?? throw new InvalidOperationException(
                        $"Интеграционное событие '{type.FullName}' не имеет обязательного атрибута [EventName].");

                var wireName = attr.Name;

                if (string.IsNullOrWhiteSpace(wireName))
                    throw new InvalidOperationException(
                        $"У события '{type.FullName}' атрибут [EventName] содержит пустое wire-имя.");

                if (!WireNameVersionRegex.IsMatch(wireName))
                {
                    throw new InvalidOperationException(
                        $"Событие '{type.FullName}' имеет недопустимый формат wire-имени '{wireName}'. " +
                        "Ожидается kebab-case / dot-separated формат с версией вида '<domain>.<event-name>.v<version>' " +
                        "(например, 'booking.cancelled.v1').");
                }

                if (nameToType.TryGetValue(wireName, out var existingType))
                {
                    throw new InvalidOperationException(
                        $"Обнаружено дублирование wire-имени '{wireName}' для типов '{existingType.FullName}' и '{type.FullName}'.");
                }

                typeToName[type] = wireName;
                nameToType[wireName] = type;
            }
        }

        _typeToName = typeToName.ToFrozenDictionary();
        _nameToType = nameToType.ToFrozenDictionary(StringComparer.Ordinal);
    }

    public string GetName<TEvent>() where TEvent : IIntegrationEvent => GetName(typeof(TEvent));

    public string GetName(Type eventType) =>
        _typeToName.TryGetValue(eventType, out var name)
            ? name
            : throw new KeyNotFoundException($"Тип '{eventType.FullName}' не зарегистрирован в реестре интеграционных событий.");

    public Type Resolve(string eventName) =>
        _nameToType.TryGetValue(eventName, out var type)
            ? type
            : throw new KeyNotFoundException($"Wire-имя события '{eventName}' не найдено в реестре интеграционных событий.");

    public bool TryResolve(string eventName, out Type? eventType) =>
        _nameToType.TryGetValue(eventName, out eventType);
}

