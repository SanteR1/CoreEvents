using System.ComponentModel.DataAnnotations;
using Events.Application.Abstractions.Caching;
using Events.Infrastructure.Caching;
using Events.Infrastructure.Caching.Options;
using Microsoft.Extensions.Configuration;
using StackExchange.Redis;

namespace Microsoft.Extensions.DependencyInjection;

internal static class RedisExtensions
{
    internal static IServiceCollection AddRedis(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<RedisOptions>()
                .Bind(configuration.GetSection(RedisOptions.SectionName))
                .ValidateDataAnnotations()
                .ValidateOnStart();

        services.AddOptions<EventCacheOptions>()
                .Bind(configuration.GetSection(EventCacheOptions.SectionName))
                .ValidateDataAnnotations()
                .ValidateOnStart();

        var redisOptions = new RedisOptions();
        configuration.GetSection(RedisOptions.SectionName).Bind(redisOptions);

        var options = new ConfigurationOptions
        {
            Password = redisOptions.Password,
            ConnectTimeout = redisOptions.ConnectTimeout,
            SyncTimeout = redisOptions.SyncTimeout,
            AbortOnConnectFail = redisOptions.AbortOnConnectFail,
            ConnectRetry = redisOptions.ConnectRetry
        };

        // Проверяем узлы ДО подключения
        if (redisOptions.EndPoints == null || !redisOptions.EndPoints.Any())
        {
            throw new ValidationException("Необходимо указать хотя бы один узел Redis.");
        }

        foreach (var endpoint in redisOptions.EndPoints)
        {
            if (string.IsNullOrWhiteSpace(endpoint))
                throw new ValidationException("Узел Redis не может быть пустым.");

            options.EndPoints.Add(endpoint);
        }
        var multiplexer = ConnectionMultiplexer.Connect(options);
        services.AddSingleton<IConnectionMultiplexer>(multiplexer);
        services.AddSingleton<ICacheService, RedisCacheService>();

        return services;
    }
}
