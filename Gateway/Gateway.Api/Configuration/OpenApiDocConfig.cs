namespace Gateway.Api;

public sealed record OpenApiDocConfig
{
    public string Key { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string Url { get; init; } = string.Empty;
}