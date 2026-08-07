using Bookings.Application.Abstractions.Persistence;
using Bookings.Application.Abstractions.Resilience.Constants;
using Bookings.Infrastructure.Resilience.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.DependencyInjection;
using Polly.Retry;

namespace Bookings.Infrastructure.Resilience.Pipelines
{
    public sealed class GlobalTransientPipelineConfig : IResiliencePipelineConfiguration
    {
        public string PipelineKey => ResiliencePipelines.GlobalTransient;

        public void Configure(ResiliencePipelineBuilder builder, AddResiliencePipelineContext<string> context)
        {
            var logger = context.ServiceProvider.GetRequiredService<ILogger<GlobalTransientPipelineConfig>>();

            var analyzerException = context.ServiceProvider.GetRequiredService<IExceptionAnalyzer>();

            builder
                //.AddTimeout(TimeSpan.FromSeconds(10)) // outer most
                .AddRetry(new RetryStrategyOptions
                {
                    // Перехватываем транзитные сбои (моргнувшая сеть, недоступность БД)
                    // ShouldHandle = new PredicateBuilder().Handle<DatabaseTransientException>(),
                    ShouldHandle = new PredicateBuilder().Handle<Exception>(analyzerException.IsTransient),
                    MaxRetryAttempts = 2,
                    BackoffType = DelayBackoffType.Constant,
                    UseJitter = true,
                    // BaseDelay = TimeSpan.FromMilliseconds(300),
                    Delay = TimeSpan.FromMilliseconds(300),
                    OnRetry = args =>
                    {
                        var exceptionMessage = args.Outcome.Exception?.Message ?? "Неизвестная ошибка";
                        logger.LogWarning(
                            args.Outcome.Exception, // Передаем сам Exception для записи StackTrace в лог
                            "[Транзитный Сбой] Попытка повтора #{Attempt}. Причина: {Message}",
                            args.AttemptNumber + 1,
                            exceptionMessage);
                        return ValueTask.CompletedTask;
                    }
                });
            //.AddTimeout(TimeSpan.FromSeconds(1)); // inner most
        }
    }
}
