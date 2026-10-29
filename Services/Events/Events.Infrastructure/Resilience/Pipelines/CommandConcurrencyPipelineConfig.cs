using Events.Application.Abstractions.Persistence;
using Events.Application.Abstractions.Resilience.Constants;
using Events.Infrastructure.Resilience.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.DependencyInjection;
using Polly.Retry;

namespace Events.Infrastructure.Resilience.Pipelines;

public sealed class CommandConcurrencyPipelineConfig : IResiliencePipelineConfiguration
{
    public string PipelineKey => ResiliencePipelines.CommandConcurrency;

    public void Configure(ResiliencePipelineBuilder builder, AddResiliencePipelineContext<string> context)
    {
        var logger = context.ServiceProvider.GetRequiredService<ILogger<CommandConcurrencyPipelineConfig>>();
        var exceptionAnalyzer = context.ServiceProvider.GetRequiredService<IExceptionAnalyzer>();

        builder.AddRetry(new RetryStrategyOptions
        {
            ShouldHandle = new PredicateBuilder().Handle<Exception>(exceptionAnalyzer.IsConcurrency),
            MaxRetryAttempts = 3,
            BackoffType = DelayBackoffType.Exponential,
            UseJitter = true,
            Delay = TimeSpan.FromMilliseconds(150),
            OnRetry = args =>
            {
                var exceptionMessage = args.Outcome.Exception?.Message ?? "Неизвестная ошибка";
                logger.LogWarning(
                    args.Outcome.Exception,
                    "[Concurrency] Конфликт версий данных. Попытка повтора #{Attempt}. Причина: {Message}",
                    args.AttemptNumber + 1,
                    exceptionMessage);
                return ValueTask.CompletedTask;
            }
        });
    }
}
