using System.Reflection;
using System.Text;
using Events.Application.Abstractions;
using Events.Application.Abstractions.Messaging;
using Events.Application.Abstractions.Persistence;
using Events.Application.Abstractions.Repositories;
using Events.Infrastructure.Data;
using Events.Infrastructure.Data.Analyzers;
using Events.Infrastructure.Data.Repositories;
using Events.Infrastructure.Extensions;
using Events.Infrastructure.Identity;
using Events.Infrastructure.Messaging;
using Events.Infrastructure.Messaging.Kafka;
using Events.Infrastructure.Messaging.Options;
using Events.Infrastructure.Tracing;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Microsoft.Extensions.DependencyInjection;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
    {
        var currentAssembly = Assembly.GetExecutingAssembly();
        services.AddResiliencePipelines(currentAssembly);

        services.AddSingleton<ICorrelationContext, CorrelationContext>();
        services.AddSingleton<IEventTopicMapper, EventTopicMapper>();

        services.AddScoped<IOutboxService, OutboxService>();

        services.AddOptions<JwtOptions>()
            .Bind(configuration.GetSection(JwtOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer();

        services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<IOptions<JwtOptions>>((bearerOptions, jwtOptionsAcc) =>
            {
                var jwtOptions = jwtOptionsAcc.Value;
                bearerOptions.MapInboundClaims = false;

                bearerOptions.TokenValidationParameters = new TokenValidationParameters
                {
                    RoleClaimType = "role",
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ClockSkew = TimeSpan.Zero,
                    ValidIssuer = jwtOptions.Issuer,
                    ValidAudience = jwtOptions.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(jwtOptions.SecretKey))
                };
            });
        services.AddHostedService<OutboxBackgroundWorker>();
        services.AddHostedService<KafkaConsumerBackgroundService>();

        services.AddOptions<KafkaOptions>()
            .Bind(configuration.GetSection("Kafka"))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddAuthorization();
        services.AddRedis(configuration);
        services.AddDataBase(configuration, environment);
        services.AddScoped<IEventRepository, EventRepository>();
        services.AddSingleton<IMessageProducer, MessageProducer>();
        services.AddScoped<IIntegrationEventDispatcher, BookingRequestDispatcher>();
        services.AddSingleton<IExceptionAnalyzer, InfrastructureExceptionAnalyzer>();
        services.AddRedis(configuration);

        return services;
    }
    private static void AddDataBase(this IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
                               ?? throw new InvalidOperationException("Connection string 'Default' not found.");

        services.AddDbContext<EventsDbContext>(options =>
        {
            options.UseNpgsql(connectionString);
            if (!environment.IsProduction())
            {
                options
                    .LogTo(Console.WriteLine)
                    .EnableDetailedErrors();
            }
        });
    }
}
