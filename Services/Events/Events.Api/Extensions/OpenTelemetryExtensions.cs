using System.Text.RegularExpressions;
using OpenTelemetry.Exporter;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace Events.Api.Extensions;

public static partial class OpenTelemetryExtensions
{
    [GeneratedRegex(@"[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}")]
    private static partial Regex GuidRegex();

    public static IServiceCollection AddApplicationTelemetry(this IServiceCollection service, IConfiguration configuration)
    {
        service.AddOpenTelemetry()
               .ConfigureResource(r => r.AddService(serviceName: "events-service"))
               .WithTracing(tracing => tracing
                                       .AddAspNetCoreInstrumentation(options =>
                                       {
                                           options.RecordException = true; // любое необработанное/throw исключение будет автоматически записано во вкладку Logs/Events текущего спана 
                                           options.Filter = httpContext =>
                                           {

                                               var path = httpContext.Request.Path;
                                               return !path.StartsWithSegments("/metrics");
                                           };
                                           options.EnrichWithHttpResponse = (activity, response) =>
                                           {
                                               var req = response.HttpContext.Request;
                                               var rawPath = req.Path.Value ?? "";
                                               var cleanRoute = GuidRegex().Replace(rawPath, "{id}");

                                               activity.DisplayName = $"{req.Method} {cleanRoute}";
                                               activity.SetTag("http.route", cleanRoute);
                                           };
                                       })
                                       .AddHttpClientInstrumentation()
                                       .AddEntityFrameworkCoreInstrumentation()
                                       .AddOtlpExporter())
               .WithMetrics(metrics => metrics
                                       .AddAspNetCoreInstrumentation()
                                       .AddRuntimeInstrumentation()
                                       .AddProcessInstrumentation()
                                       .AddPrometheusExporter(options =>
                                       {
                                           // Жестко фиксируем стратегию: всегда менять точки на подчеркивания
                                           // и добавлять суффиксы (например, _total, _count), 
                                           // игнорируя заголовки Content Negotiation от Prometheus.
                                           options.TranslationStrategy = PrometheusAspNetCoreTranslationStrategy.UnderscoreEscapingWithSuffixes;
                                       }));

        return service;
    }

}
