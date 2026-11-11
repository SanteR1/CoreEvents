using System.Reflection;
using Events.Application.Abstractions.Messaging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Events.Application.Extensions;

public static class DomainEventRegistrationExtensions
{
    public static IServiceCollection AddDomainEventHandlers(
        this IServiceCollection services,
        Assembly assembly)
    {
        var definitions = new HashSet<Type>
        {
            typeof(IPreCommitHandler<>),
            typeof(IPostCommitHandler<>)
        };

        var implementations = assembly.DefinedTypes
            .Where(t => t.IsClass && !t.IsAbstract && !t.ContainsGenericParameters)
            .OrderBy(t => t.FullName, StringComparer.Ordinal);

        foreach (var implementation in implementations)
        {
            var handlerInterfaces = implementation
                .GetInterfaces()
                .Where(i =>
                    i.IsGenericType &&
                    definitions.Contains(i.GetGenericTypeDefinition()))
                .Distinct()
                .ToArray();

            if (handlerInterfaces.Length == 0)
                continue;

            var implementationType = implementation.AsType();

            services.TryAdd(
                ServiceDescriptor.Scoped(
                    implementationType,
                    implementationType));

            foreach (var handlerInterface in handlerInterfaces)
            {
                // Используем AddScoped вместо TryAddEnumerable:
                // Для фабричных дескрипторов TryAddEnumerable не видит тип реализации (он равен null)
                // и отбросил бы второй независимый обработчик того же события как дубликат.
                services.AddScoped(
                    handlerInterface,
                    sp => sp.GetRequiredService(implementationType));
            }
        }

        return services;
    }
}
