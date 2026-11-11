using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Events.Application.Abstractions.Messaging;
using Events.Domain.DomainEvents;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Events.Application.Common.Dispatcher;

internal sealed class DomainEventDispatcher(
    IServiceProvider serviceProvider,
    ILogger<DomainEventDispatcher> logger)
    : IDomainEventDispatcher
{
    private readonly IServiceProvider _serviceProvider = serviceProvider;
    private readonly ILogger<DomainEventDispatcher> _logger = logger;

    private delegate Task DispatchDelegate(
        DomainEventDispatcher dispatcher,
        IDomainEvent domainEvent,
        CancellationToken ct);

    private static readonly ConcurrentDictionary<Type, DispatchDelegate> PreCommitCache = new();
    private static readonly ConcurrentDictionary<Type, DispatchDelegate> PostCommitCache = new();

    public Task DispatchPreCommitAsync(
        IDomainEvent domainEvent,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);

        var dispatch = PreCommitCache.GetOrAdd(
            domainEvent.GetType(),
            static type => CreateDelegate(
                nameof(DispatchPreCommitTypedAsync),
                type));

        return dispatch(this, domainEvent, ct);
    }

    public Task DispatchPostCommitAsync(
        IDomainEvent domainEvent,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);

        var dispatch = PostCommitCache.GetOrAdd(
            domainEvent.GetType(),
            static type => CreateDelegate(
                nameof(DispatchPostCommitTypedAsync),
                type));

        return dispatch(this, domainEvent, ct);
    }

    [SuppressMessage("SonarQube", "S3011:Reflection should not be used to increase accessibility of classes, methods, or fields", Justification = "Performance-optimized generic delegate construction for typed domain event handlers")]
    private static DispatchDelegate CreateDelegate(
        string methodName,
        Type eventType)
    {
        var method = typeof(DomainEventDispatcher)
            .GetMethod(
                methodName,
                BindingFlags.NonPublic | BindingFlags.Static)
            ?? throw new MissingMethodException(nameof(DomainEventDispatcher), methodName);

        return method
            .MakeGenericMethod(eventType)
            .CreateDelegate<DispatchDelegate>();
    }

    private static async Task DispatchPreCommitTypedAsync<TEvent>(
        DomainEventDispatcher dispatcher,
        IDomainEvent domainEvent,
        CancellationToken ct)
        where TEvent : IDomainEvent
    {
        var handlers = dispatcher._serviceProvider
            .GetServices<IPreCommitHandler<TEvent>>()
            .ToList();

        if (handlers.Count == 0)
        {
            dispatcher._logger.LogDebug(
                "Для события {EventType} (PreCommit) обработчики не зарегистрированы. Пропуск.",
                typeof(TEvent).Name);
            return;
        }

        foreach (var handler in handlers)
        {
            ct.ThrowIfCancellationRequested();

            await handler.Handle((TEvent)domainEvent, ct);
        }
    }

    private static async Task DispatchPostCommitTypedAsync<TEvent>(
        DomainEventDispatcher dispatcher,
        IDomainEvent domainEvent,
        CancellationToken ct)
        where TEvent : IDomainEvent
    {
        var handlers = dispatcher._serviceProvider
            .GetServices<IPostCommitHandler<TEvent>>()
            .ToList();

        if (handlers.Count == 0)
        {
            dispatcher._logger.LogDebug(
                "Для события {EventType} (PostCommit) обработчики не зарегистрированы. Пропуск.",
                typeof(TEvent).Name);
            return;
        }

        foreach (var handler in handlers)
        {
            ct.ThrowIfCancellationRequested();

            await handler.Handle((TEvent)domainEvent, ct);
        }
    }
}
