using Bookings.Application.Abstractions.Resilience.Constants;
using Bookings.Domain.Exceptions;
using Bookings.Infrastructure.Resilience.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.DependencyInjection;
using Polly.Retry;

namespace Bookings.Infrastructure.Resilience.Pipelines
{
    public sealed class CommandConcurrencyPipelineConfig : IResiliencePipelineConfiguration
    {
        public string PipelineKey => ResiliencePipelines.CommandConcurrency;

        public void Configure(ResiliencePipelineBuilder builder, AddResiliencePipelineContext<string> context)
        {
            var logger = context.ServiceProvider.GetRequiredService<ILogger<CommandConcurrencyPipelineConfig>>();

            builder
                //.AddTimeout(TimeSpan.FromSeconds(10)) // outer most
                .AddRetry(new RetryStrategyOptions
                {
                    ShouldHandle = new PredicateBuilder().Handle<ConcurrencyException>(),
                    MaxRetryAttempts = 3,
                    BackoffType = DelayBackoffType.Exponential,
                    UseJitter = true, // Размываем пики, чтобы потоки не штурмовали Postgres одновременно
                    // BaseDelay = TimeSpan.FromMilliseconds(150),
                    Delay = TimeSpan.FromMilliseconds(150),
                    OnRetry = args =>
                    {
                        var exceptionMessage = args.Outcome.Exception?.Message ?? "Неизвестная ошибка";
                        logger.LogWarning(
                            args.Outcome.Exception, // Передаем сам Exception для записи StackTrace в лог
                            "[Concurrency] Конфликт версий данных. Попытка повтора #{Attempt}. Причина: {Message}",
                            args.AttemptNumber + 1,
                            exceptionMessage);
                        return ValueTask.CompletedTask;
                    }
                });
            //.AddTimeout(TimeSpan.FromSeconds(1)); // inner most
        }
    }
}
