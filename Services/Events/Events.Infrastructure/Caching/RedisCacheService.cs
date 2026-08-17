using System.Text.Json;
using Events.Application.Abstractions.Caching;
using Events.Infrastructure.Caching.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace Events.Infrastructure.Caching;

internal class RedisCacheService(
    IConnectionMultiplexer connectionMultiplexer,
    IOptions<EventCacheOptions> options,
    ILogger<RedisCacheService> logger
) : ICacheService
{
    private readonly IDatabase _db = connectionMultiplexer.GetDatabase();
    private readonly EventCacheOptions _cacheOptions = options.Value;

    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellation = default)
    {
        try
        {
            var cachedValue = await _db.StringGetAsync(key);

            if (!cachedValue.HasValue)
                return default;

            return JsonSerializer.Deserialize<T>(cachedValue.ToString());
        }
        catch (Exception ex) when (ex is RedisConnectionException or RedisTimeoutException
                                       or RedisServerException)
        {
            logger.LogWarning(ex,
                "Redis StringGetAsync failed for key {Key}, falling back to DB",
                key);

            return default;
        }
        catch (JsonException ex)
        {
            logger.LogWarning(ex, "Failed to deserialize cached value for key {Key}", key);

            return default;
        }
    }

    public async Task SetAsync<T>(string key, T value, CancellationToken ct = default)
    {
        try
        {
            await _db.StringSetAsync(
                key,
                JsonSerializer.Serialize(value),
                GetTtlForKey(key));
        }
        catch (Exception ex) when (ex is RedisConnectionException or RedisTimeoutException
                                       or RedisServerException)
        {
            logger.LogWarning(ex,
                "Redis StringSetAsync failed for key {Key}, skipping cache write",
                key);
        }
    }

    public async Task DeleteAsync(string key, CancellationToken ct = default)
    {
        try
        {
            await _db.KeyDeleteAsync(key);
        }
        catch (Exception ex) when (ex is RedisConnectionException or RedisTimeoutException
                                       or RedisServerException)
        {
            logger.LogWarning(ex, "Redis KeyDeleteAsync failed for key {Key}", key);
        }
    }

    private TimeSpan GetTtlForKey(string key)
    {
        if (key.Contains(":top", StringComparison.OrdinalIgnoreCase))
        {
            return _cacheOptions.EventTopTtl;
        }

        return _cacheOptions.EventTtl;
    }
}
