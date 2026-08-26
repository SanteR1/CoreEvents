using System.Diagnostics.Metrics;
using Bookings.Api.Extensions;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

namespace Bookings.Tests.Extensions;

public class InfrastructureExtensionsTests
{
    [Fact]
    public void AddApplicationTelemetry_Registers_TracerAndMeterProviders()
    {
        // Arrange
        var builder = WebApplication.CreateBuilder();

        // Act
        builder.Services.AddApplicationTelemetry(builder.Configuration);
        using var app = builder.Build();

        // Assert
        // Проверяем регистрацию провайдера метрик OpenTelemetry
        var meterProvider = app.Services.GetService<MeterProvider>();
        Assert.NotNull(meterProvider);

        // Проверяем регистрацию провайдера трейсинга OpenTelemetry
        var tracerProvider = app.Services.GetService<TracerProvider>();
        Assert.NotNull(tracerProvider);

        // Убеждаемся, что стандартная фабрика метрик .NET также доступна
        var meterFactory = app.Services.GetService<IMeterFactory>();
        Assert.NotNull(meterFactory);
    }

    [Fact]
    public void AddApplicationLogging_RegistersSerilog_AsLogger()
    {
        // Arrange
        var builder = WebApplication.CreateBuilder();

        // Act
        builder.AddApplicationLogging();
        using var app = builder.Build();

        // Assert
        // Проверяем, что фабрика логгеров успешно зарегистрирована
        var loggerFactory = app.Services.GetService<ILoggerFactory>();
        Assert.NotNull(loggerFactory);

        // Создаем логгер и убеждаемся, что пайплайн не падает
        var logger = loggerFactory.CreateLogger<InfrastructureExtensionsTests>();
        Assert.NotNull(logger);

        // Убеждаемся, что провайдером по умолчанию стал Serilog
        Assert.Contains("Serilog", loggerFactory.GetType().FullName);
    }
}
