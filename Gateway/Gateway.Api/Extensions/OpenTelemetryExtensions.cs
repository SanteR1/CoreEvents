using System.Text.RegularExpressions;
using OpenTelemetry.Exporter;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Yarp.ReverseProxy.Model;

namespace Gateway.Api.Extensions;

public static partial class OpenTelemetryExtensions
{
    [GeneratedRegex(@"[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}")]
    private static partial Regex GuidRegex();

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
                    options.EnrichWithHttpResponse = (activity, response) =>
                    {
                        var req = response.HttpContext.Request;
                        var rawPath = req.Path.Value ?? "";
                        var cleanRoute = GuidRegex().Replace(rawPath, "{id}");

                        activity.DisplayName = $"{req.Method} {cleanRoute}";
                        activity.SetTag("http.route", cleanRoute);

                        var proxyFeature = response.HttpContext.GetReverseProxyFeature();
                        if (proxyFeature?.Route?.Config?.RouteId is { } routeId)
                        {
                            activity.SetTag("gateway.route_id", routeId);
                        }
                        if (proxyFeature?.ProxiedDestination?.Model?.Config?.Address is { } destAddress)
                        {
                            activity.SetTag("gateway.destination", destAddress);
                        }
                    };
                })
                .AddHttpClientInstrumentation(options =>
                {
                    options.EnrichWithHttpRequestMessage = (activity, request) =>
                    {
                        var rawPath = request.RequestUri?.AbsolutePath ?? "";
                        var cleanRoute = GuidRegex().Replace(rawPath, "{id}");
                        activity.DisplayName = $"{request.Method} {cleanRoute}";
                        activity.SetTag("http.route", cleanRoute);
                    };
                })
                .AddSource("Yarp.ReverseProxy")
                .AddOtlpExporter())
            .WithMetrics(metrics => metrics
                .AddAspNetCoreInstrumentation()
                .AddRuntimeInstrumentation()
                .AddProcessInstrumentation()
                .AddMeter("Yarp.ReverseProxy")
                .AddPrometheusExporter(options =>
                {
                    // Жестко фиксируем стратегию: всегда менять точки на подчеркивания
                    // и добавлять суффиксы (например, _total, _count), 
                    // игнорируя заголовки Content Negotiation от Prometheus.
                    options.TranslationStrategy = PrometheusAspNetCoreTranslationStrategy.UnderscoreEscapingWithSuffixes;
                }));

        return services;
    }
}
