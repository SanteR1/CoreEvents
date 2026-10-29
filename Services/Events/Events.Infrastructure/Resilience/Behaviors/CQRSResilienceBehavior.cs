using System.Collections.Concurrent;
using System.Reflection;
using Events.Application.Abstractions.Messaging;
using Events.Application.Abstractions.Resilience.Attributes;
using Events.Application.Abstractions.Resilience.Constants;
using MediatR;
using Polly.Registry;

namespace Events.Infrastructure.Resilience.Behaviors;

public sealed class CqrsResilienceBehavior<TRequest, TResponse>(
    ResiliencePipelineProvider<string> pipelineProvider)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private static readonly ConcurrentDictionary<Type, string[]> _pipelineCache = new();

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var requestType = typeof(TRequest);
        var pipelineKeys = _pipelineCache.GetOrAdd(requestType, GetKeysFromAttributes);

        // 1. Если атрибутов нет, просто вызываем next, передав только токен
        if (pipelineKeys.Length == 0)
        {
            return await next(cancellationToken);
        }

        // 2. Строим цепочку (матрешку)
        // Начинаем с исходного делегата MediatR
        RequestHandlerDelegate<TResponse> currentDelegate = next;

        // Оборачиваем каждый пайплайн вокруг текущего делегата
        for (int i = pipelineKeys.Length - 1; i >= 0; i--)
        {
            var key = pipelineKeys[i];
            var pipeline = pipelineProvider.GetPipeline(key);
            var previousDelegate = currentDelegate;

            // Создаем новый делегат, который соответствует сигнатуре: (CancellationToken t)
            currentDelegate = async (token) =>
            {
                // Выполняем пайплайн Polly, а внутри него вызываем предыдущий шаг
                return await pipeline.ExecuteAsync(
                    async (innerToken) => await previousDelegate(innerToken),
                    token);
            };
        }

        // 3. Запускаем итоговую цепочку
        return await currentDelegate(cancellationToken);
    }

    private static string[] GetKeysFromAttributes(Type type)
    {
        var multiAttr = type.GetCustomAttribute<ResiliencePipelinesAttribute>();
        if (multiAttr != null) return multiAttr.Keys;

        var attrs = type.GetCustomAttributes<ResiliencePipelineAttribute>()
            .Select(a => a.Key)
            .ToArray();

        if (attrs.Length > 0) return attrs;

        // Конвенция по умолчанию: все IInboxCommand автоматически защищены ретраями конкуренции (40001 / 40P01 / DbUpdateConcurrencyException)
        if (typeof(IInboxCommand).IsAssignableFrom(type))
        {
            return [ResiliencePipelines.CommandConcurrency];
        }

        return [];
    }
}
