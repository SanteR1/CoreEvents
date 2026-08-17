using System.ComponentModel.DataAnnotations;

namespace Events.Infrastructure.Caching.Options;

internal sealed record RedisOptions
{
    public const string SectionName = "Redis";
    [MinLength(1, ErrorMessage = "Необходимо указать хотя бы один узел Redis.")]
    public string[] EndPoints { get; init; } = Array.Empty<string>();
    [Range(0, int.MaxValue)]
    public int ConnectTimeout { get; init; } = 5000;
    [Range(0, int.MaxValue)]
    public int SyncTimeout { get; init; } = 3000;
    [Range(0, int.MaxValue)]
    public int ConnectRetry { get; init; } = 3;
    public bool AbortOnConnectFail { get; init; } = false;
}
