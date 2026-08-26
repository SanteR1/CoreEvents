using System.Diagnostics.Metrics;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using Users.Api.Extensions;

namespace Users.Tests.Extensions;

public class InfrastructureExtensionsTests
{
    [Fact]
    public void AddApplicationLogging_RegistersSerilog_AsLogger()
    {
        // Arrange
        WebApplicationBuilder builder = WebApplication.CreateBuilder();

        // Act
        builder.AddApplicationLogging();
        using WebApplication app = builder.Build();

        // Assert
        // Проверяем, что фабрика логгеров успешно зарегистрирована
        ILoggerFactory? loggerFactory = app.Services.GetService<ILoggerFactory>();
        Assert.NotNull(loggerFactory);

        // Создаем логгер и убеждаемся, что пайплайн не падает
        ILogger<InfrastructureExtensionsTests> logger = loggerFactory.CreateLogger<InfrastructureExtensionsTests>();
        Assert.NotNull(logger);

        // Убеждаемся, что провайдером по умолчанию стал Serilog
        Assert.Contains("Serilog", loggerFactory.GetType().FullName);
    }

    [Fact]
    public void AddApplicationTelemetry_Registers_TracerAndMeterProviders()
    {
        // Arrange
        WebApplicationBuilder builder = WebApplication.CreateBuilder();

        // Act
        builder.Services.AddApplicationTelemetry(builder.Configuration);
        using WebApplication app = builder.Build();

        // Assert
        // Проверяем регистрацию провайдера метрик OpenTelemetry
        MeterProvider? meterProvider = app.Services.GetService<MeterProvider>();
        Assert.NotNull(meterProvider);

        // Проверяем регистрацию провайдера трейсинга OpenTelemetry
        TracerProvider? tracerProvider = app.Services.GetService<TracerProvider>();
        Assert.NotNull(tracerProvider);

        // Убеждаемся, что стандартная фабрика метрик .NET также доступна
        IMeterFactory? meterFactory = app.Services.GetService<IMeterFactory>();
        Assert.NotNull(meterFactory);
    }
}
