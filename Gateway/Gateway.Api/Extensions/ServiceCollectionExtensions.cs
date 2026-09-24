using System.Threading.RateLimiting;
using Gateway.Api.Configuration;
using Gateway.Api.Transforms;
using Microsoft.AspNetCore.RateLimiting;
using Yarp.ReverseProxy.Transforms.Builder;

namespace Gateway.Api.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddGatewayCors(this IServiceCollection services, IConfiguration configuration)
    {
        var corsOptions = configuration.GetSection("Cors").Get<CorsOptions>() ?? new CorsOptions();

        services.AddCors(options =>
        {
            options.AddPolicy("CorsPolicy", policy =>
            {
                policy.WithOrigins(corsOptions.AllowedOrigins)
                      .AllowAnyHeader()
                      .AllowAnyMethod()
                      .AllowCredentials();
            });
        });

        return services;
    }

    public static IServiceCollection AddGatewayRateLimiting(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<AuthRateLimitingOptions>(configuration.GetSection("RateLimiting:Auth"));

        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.AddPolicy("auth-policy", httpContext =>
            {
                var rateLimitOptions = httpContext.RequestServices
                    .GetRequiredService<Microsoft.Extensions.Options.IOptions<AuthRateLimitingOptions>>().Value;

                return RateLimitPartition.GetSlidingWindowLimiter(
                    partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "anonymous",
                    factory: _ => new SlidingWindowRateLimiterOptions
                    {
                        PermitLimit = rateLimitOptions.PermitLimit,
                        Window = TimeSpan.FromSeconds(rateLimitOptions.WindowSeconds),
                        SegmentsPerWindow = rateLimitOptions.SegmentsPerWindow,
                        QueueLimit = rateLimitOptions.QueueLimit
                    });
            });
        });

        return services;
    }

    public static IServiceCollection AddGatewayDocumentation(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        if (!environment.IsDevelopment())
        {
            return services;
        }

        services.Configure<List<OpenApiDocConfig>>(configuration.GetSection("OpenApiDocs"));

        var localPort = configuration["ASPNETCORE_HTTP_PORTS"] ?? "8080";
        services.AddHttpClient("openapi-validator", client =>
        {
            client.BaseAddress = new Uri($"http://localhost:{localPort}");
        });

        services.AddHostedService<OpenApiDocsValidator>();

        return services;
    }

    public static IServiceCollection AddGatewayReverseProxy(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<ITransformProvider, CookieToBearerTransformProvider>();
        services.AddReverseProxy()
            .LoadFromConfig(configuration.GetSection("ReverseProxy"));

        return services;
    }
}