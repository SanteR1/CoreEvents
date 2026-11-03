using Events.Infrastructure.Messaging;
using Events.IntegrationTests.Infrastructure.Auth;
using Events.IntegrationTests.Infrastructure.Logging;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog.Core;

namespace Events.IntegrationTests.Infrastructure.Factories;


/// <summary>
/// Запуск без фонового обработчика Kafka/Redis
/// </summary>
public class ApiOnlyIntegrationTestFactory : IntegrationTestFactory
{
    public TestConcurrencyLogCollector ConcurrencyLogCollector { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);

        builder.ConfigureAppConfiguration((context, configBuilder) =>
        {
            var testConfig = new Dictionary<string, string?>(StringComparer.Ordinal)
            {
                { "Kafka:InitKafkaTopics", "false" }
            };
            configBuilder.AddInMemoryCollection(testConfig);
        });

        Environment.SetEnvironmentVariable("Redis__ConnectTimeout", "0");
        Environment.SetEnvironmentVariable("Redis__SyncTimeout", "0");
        Environment.SetEnvironmentVariable("Redis__ConnectRetry", "0");

        builder.ConfigureTestServices(services =>
        {
            // Регистрация коллектора логов для отслеживания ретраев конкуренции
            services.AddSingleton<ILogEventSink>(ConcurrencyLogCollector);

            // 1. Отключаем фоновые воркеры
            var workersToRemove = new[]
            {
                typeof(OutboxBackgroundWorker),
                typeof(KafkaConsumerBackgroundService)
            };

            var descriptors = services
                              .Where(d => d.ServiceType == typeof(IHostedService) &&
                                          d.ImplementationType != null &&
                                          workersToRemove.Contains(d.ImplementationType))
                              .ToList();

            foreach (var descriptor in descriptors)
            {
                services.Remove(descriptor);
            }

            // 2. Принудительно делаем тестовую схему главной
            services.Configure<AuthenticationOptions>(options =>
            {
                options.DefaultAuthenticateScheme = TestAuthHandler.AuthenticationScheme;
                options.DefaultChallengeScheme = TestAuthHandler.AuthenticationScheme;
                options.DefaultScheme = TestAuthHandler.AuthenticationScheme;
            });
            services.AddAuthentication(TestAuthHandler.AuthenticationScheme)
                    .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(
                        TestAuthHandler.AuthenticationScheme, options => { });
        });
    }
}
