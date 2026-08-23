using OpenTelemetry.Exporter;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace Bookings.Api.Extensions;

public static class OpenTelemetryExtensions
{
    public static IServiceCollection AddApplicationTelemetry(this IServiceCollection service, IConfiguration configuration)
    {
        service.AddOpenTelemetry()
               .ConfigureResource(r => r.AddService(serviceName: "bookings-service"))
               .WithTracing(tracing => tracing
                                       .AddAspNetCoreInstrumentation(options =>
                                       {
                                           options.RecordException = true; // любое необработанное/throw исключение будет автоматически записано во вкладку Logs/Events текущего спана 
                                           options.Filter = httpContext =>
                                           {
                                               
                                               var path = httpContext.Request.Path;
                                               return !path.StartsWithSegments("/health") &&
                                                      !path.StartsWithSegments("/metrics");
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
                                           options.TranslationStrategy = PrometheusTranslationStrategy.UnderscoreEscapingWithSuffixes;
                                       }));

        return service;
    }

}
