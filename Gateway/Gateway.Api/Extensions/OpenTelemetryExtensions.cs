using OpenTelemetry.Exporter;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace Gateway.Api.Extensions;

public static class OpenTelemetryExtensions
{
    public static IServiceCollection AddGatewayTelemetry(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOpenTelemetry()
            .ConfigureResource(resource => resource.AddService(serviceName: "gateway-service"))
            .WithTracing(tracing => tracing
                .AddAspNetCoreInstrumentation(options =>
                {
                    options.RecordException = true;
                    options.Filter = httpContext =>
                    {
                        var path = httpContext.Request.Path;
                        return !path.StartsWithSegments("/metrics")
                            && !path.StartsWithSegments("/swagger")
                            && !path.StartsWithSegments("/docs");
                    };
                })
                .AddHttpClientInstrumentation()
                .AddSource("Yarp.ReverseProxy")
                .AddOtlpExporter())
            .WithMetrics(metrics => metrics
                .AddAspNetCoreInstrumentation()
                .AddRuntimeInstrumentation()
                .AddProcessInstrumentation()
                .AddMeter("Yarp.ReverseProxy")
                .AddPrometheusExporter());

        return services;
    }
}
