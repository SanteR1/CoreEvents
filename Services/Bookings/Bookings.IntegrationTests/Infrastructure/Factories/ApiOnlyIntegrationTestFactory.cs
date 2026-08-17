using Bookings.Infrastructure.Messaging;
using Bookings.IntegrationTests.Infrastructure.Auth;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Bookings.IntegrationTests.Infrastructure.Factories;


/// <summary>
/// Запуск без фонового обработчика Kafka/Redis
/// </summary>
public class ApiOnlyIntegrationTestFactory : IntegrationTestFactory
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);

        builder.ConfigureAppConfiguration((context, configBuilder) =>
        {
            var testConfig = new Dictionary<string, string?>
            {
                { "Kafka:InitKafkaTopics", "false" }
            };
            configBuilder.AddInMemoryCollection(testConfig);
        });


        builder.ConfigureTestServices(services =>
        {
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
