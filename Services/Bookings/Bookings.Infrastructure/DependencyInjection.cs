using System.Reflection;
using System.Text;
using Bookings.Application.Abstractions;
using Bookings.Application.Abstractions.Messaging;
using Bookings.Application.Abstractions.Persistence;
using Bookings.Application.Abstractions.Repositories;
using Bookings.Infrastructure.Analyzers;
using Bookings.Infrastructure.Data;
using Bookings.Infrastructure.Extensions;
using Bookings.Infrastructure.Identity;
using Bookings.Infrastructure.Messaging;
using Bookings.Infrastructure.Messaging.Kafka;
using Bookings.Infrastructure.Messaging.Options;
using Bookings.Infrastructure.Repositories;
using Bookings.Infrastructure.Tracing;
using Confluent.Kafka;
using Confluent.Kafka.Admin;
using CoreEvents.Shared.Contracts.Events;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
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
        services.AddDataBase(configuration, environment);

        services.AddScoped<IBookingRepository, BookingRepository>();

        services.AddScoped<IIntegrationEventDispatcher, EventServiceResponseDispatcher>();

        services.AddSingleton<IMessageProducer, MessageProducer>();

        services.AddSingleton<IExceptionAnalyzer, InfrastructureExceptionAnalyzer>();
        return services;
    }
    private static IServiceCollection AddDataBase(this IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
                               ?? throw new InvalidOperationException("Connection string 'Default' not found.");

        services.AddDbContext<BookingsDbContext>(options =>
        {
            options.UseNpgsql(connectionString);
            if (!environment.IsProduction())
            {
                options
                    .LogTo(Console.WriteLine)
                    .EnableDetailedErrors();
            }
        });

        return services;
    }

    public static async Task InitializeKafkaTopicsAsync(this IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<AdminClientBuilder>>();

        var kafkaOptions = scope.ServiceProvider.GetRequiredService<IOptions<KafkaOptions>>().Value;

        var config = new AdminClientConfig
        {
            BootstrapServers = kafkaOptions.BootstrapServers
        };

        logger.LogInformation("Начало проверки и создания топиков Kafka...");

        using var adminClient = new AdminClientBuilder(config).Build();

        var topicsToCreate = new List<TopicSpecification>
        {
            new TopicSpecification
            {
                Name = KafkaTopics.EventConfirmed,
                NumPartitions = kafkaOptions.Topics.MainTopic.Partitions,
                ReplicationFactor = kafkaOptions.Topics.MainTopic.ReplicationFactor
            },
            new TopicSpecification
            {
                Name = KafkaTopics.EventConfirmedDlt,
                NumPartitions = kafkaOptions.Topics.DeadLetterTopic.Partitions,
                ReplicationFactor = kafkaOptions.Topics.DeadLetterTopic.ReplicationFactor
            }
        };

        try
        {
            var options = new CreateTopicsOptions
            {
                RequestTimeout = TimeSpan.FromSeconds(10)
            };

            await adminClient.CreateTopicsAsync(topicsToCreate, options);
            logger.LogInformation("Все требуемые топики Kafka успешно созданы.");
        }
        catch (CreateTopicsException e)
        {
            foreach (var result in e.Results)
            {
                if (result.Error.Code == ErrorCode.TopicAlreadyExists)
                {
#pragma warning disable S6667 // TopicAlreadyExists is expected when topics were previously created
                    logger.LogInformation("Топик '{Topic}' уже существует. Пропускаем.", result.Topic);
#pragma warning restore S6667
                }
                else
                {
                    logger.LogError(e, "Ошибка при создании топика '{Topic}': {Error}", result.Topic, result.Error.Reason);
                    throw new InvalidOperationException($"Ошибка при создании топика '{result.Topic}': {result.Error.Reason}", e);
                }
            }
        }
        catch (KafkaException ex) when (ex.Error.Code == ErrorCode.Local_TimedOut)
        {
            logger.LogCritical(ex, "Не удалось связаться с Kafka по таймауту при создании топиков.");
            throw new InvalidOperationException("Не удалось связаться с Kafka по таймауту при создании топиков.", ex);
        }
        catch (Exception ex)
        {
            logger.LogCritical(ex, "Критическая ошибка при инициализации Kafka. Приложение будет остановлено.");
            throw new InvalidOperationException("Критическая ошибка при инициализации Kafka.", ex);
        }
    }
}
