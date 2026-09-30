namespace Gateway.Api.Configuration;

public sealed class AuthRateLimitingOptions
{
    public int PermitLimit { get; init; } = 10;
    public int WindowSeconds { get; init; } = 60;
    public int SegmentsPerWindow { get; init; } = 2;
    public int QueueLimit { get; init; }
}
