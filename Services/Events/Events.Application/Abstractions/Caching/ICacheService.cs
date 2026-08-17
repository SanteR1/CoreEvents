namespace Events.Application.Abstractions.Caching;

public interface ICacheService
{
    Task<T?> GetAsync<T>(string key, CancellationToken cancellation = default);
    Task SetAsync<T>(string key, T value, CancellationToken cancellation = default);
    Task DeleteAsync(string key, CancellationToken cancellation = default);
}
