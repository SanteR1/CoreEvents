using Gateway.Api;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Gateway.Tests.Fixtures;

public class GatewayWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _environment;
    private readonly Action<IServiceCollection>? _configureServices;
    private readonly IDictionary<string, string?>? _configurationOverrides;

    public GatewayWebApplicationFactory(
        string environment = "Development",
        Action<IServiceCollection>? configureServices = null,
        IDictionary<string, string?>? configurationOverrides = null)
    {
        _environment = environment;
        _configureServices = configureServices;
        _configurationOverrides = configurationOverrides;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment(_environment);

        if (_configurationOverrides != null && _configurationOverrides.Count > 0)
        {
            builder.ConfigureAppConfiguration((_, config) =>
            {
                config.AddInMemoryCollection(_configurationOverrides);
            });
        }

        builder.ConfigureServices(services =>
        {
            // Отключаем фоновую startup-валидацию в тестах для ускорения выполнения,
            // если только она явно не тестируется
            var validatorDescriptor = services.FirstOrDefault(d => d.ImplementationType == typeof(OpenApiDocsValidator));
            if (validatorDescriptor != null)
            {
                services.Remove(validatorDescriptor);
            }

            _configureServices?.Invoke(services);
        });
    }
}
