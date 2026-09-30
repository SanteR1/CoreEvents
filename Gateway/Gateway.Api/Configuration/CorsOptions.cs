namespace Gateway.Api.Configuration;

public sealed class CorsOptions
{
    public string[] AllowedOrigins { get; init; } = ["http://localhost:5173"];
}

