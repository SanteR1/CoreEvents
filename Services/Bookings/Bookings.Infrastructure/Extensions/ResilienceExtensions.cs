using System.Reflection;
using Bookings.Infrastructure.Resilience.Abstractions;
using Bookings.Infrastructure.Resilience.Behaviors;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Polly;

namespace Bookings.Infrastructure.Extensions;

internal static class ResilienceExtensions
{
    public static void AddResiliencePipelines(this IServiceCollection services, Assembly assembly)
    {
        services.AddResiliencePipelineRegistry<string>();

        // Сканируем сборку на наличие конфигураций пайплайнов
        var pipelineConfigTypes = assembly.GetTypes()
            .Where(t => typeof(IResiliencePipelineConfiguration).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract)
            .ToList();

        foreach (var configType in pipelineConfigTypes)
        {
            // Сначала регистрируем сам класс конфигурации в DI (как Transient)
            // Это нужно, чтобы внутри AddResiliencePipeline мы могли достать его через GetRequiredService
            services.AddTransient(configType);

            // Используем временный экземпляр только для того, чтобы узнать ключ пайплайна
            // В Polly v8 ключ является обязательным параметром при регистрации
            var tempInstance = (IResiliencePipelineConfiguration)Activator.CreateInstance(configType)!;
            var pipelineKey = tempInstance.PipelineKey;

            services.AddResiliencePipeline(pipelineKey, (builder, context) =>
            {
                // Достаем полноценный экземпляр конфигуратора из DI.
                // Это гарантирует, что все зависимости (Logger, ExceptionAnalyzer и т.д.) 
                // будут прокинуты в конструктор конфигуратора правильно.
                var configurator = context.ServiceProvider.GetRequiredService(configType)
                    as IResiliencePipelineConfiguration;

                configurator?.Configure(builder, context);
            });
        }
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(CqrsResilienceBehavior<,>));
    }
}
