using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Users.IntegrationTests.Infrastructure.Factories;

/// <summary>
///     Запуск без фонового обработчика
/// </summary>
public class ApiOnlyIntegrationTestFactory : IntegrationTestFactory
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);

        builder.ConfigureAppConfiguration((context, configBuilder) =>
        {
            Dictionary<string, string?> testConfig = new() { { "Kafka:InitKafkaTopics", "false" } };
            configBuilder.AddInMemoryCollection(testConfig);
        });

        builder.ConfigureTestServices(services =>
        {
            // Указываем ASP.NET Core искать контроллеры в сборке с тестами
            services.AddControllers()
                    .AddApplicationPart(typeof(TestAuthController).Assembly);
        });
    }
}
