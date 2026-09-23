using System.Net;
using Gateway.Tests.Fixtures;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

namespace Gateway.Tests.Integration;

public class MetricsTests
{
    [Fact]
    public async Task GetMetrics_ReturnsOkAndPrometheusFormattedMetrics()
    {
        // Arrange
        using var factory = new GatewayWebApplicationFactory();
        using var client = factory.CreateClient();

        // Act
        var response = await client.GetAsync("/metrics", TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.Should().Be("text/plain");

        var content = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        content.Should().NotBeNullOrWhiteSpace();
        // Проверяем наличие стандартных аннотаций Prometheus метрик (# HELP или # TYPE)
        content.Should().Contain("# TYPE");
    }

    [Fact]
    public void OpenTelemetryProviders_AreRegisteredInDependencyInjection()
    {
        // Arrange
        using var factory = new GatewayWebApplicationFactory();

        // Act
        var tracerProvider = factory.Services.GetService<TracerProvider>();
        var meterProvider = factory.Services.GetService<MeterProvider>();

        // Assert
        tracerProvider.Should().NotBeNull();
        meterProvider.Should().NotBeNull();
    }
}
