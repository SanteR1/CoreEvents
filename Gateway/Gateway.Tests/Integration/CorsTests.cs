using System.Net;
using Gateway.Tests.Fixtures;

namespace Gateway.Tests.Integration;

public class CorsTests
{
    private const string AllowedOrigin = "http://localhost:5173";
    private const string DisallowedOrigin = "http://malicious-domain.com";

    [Fact]
    public async Task Request_WithAllowedOrigin_ReturnsCorsHeadersAndCredentials()
    {
        // Arrange
        using var factory = new GatewayWebApplicationFactory();
        using var client = factory.CreateClient();

        using var request = new HttpRequestMessage(HttpMethod.Get, "/v1/events");
        request.Headers.Add("Origin", AllowedOrigin);

        // Act
        using var response = await client.SendAsync(request, TestContext.Current.CancellationToken);

        // Assert
        response.Headers.Contains("Access-Control-Allow-Origin").Should().BeTrue();
        response.Headers.GetValues("Access-Control-Allow-Origin").First().Should().Be(AllowedOrigin);

        response.Headers.Contains("Access-Control-Allow-Credentials").Should().BeTrue();
        response.Headers.GetValues("Access-Control-Allow-Credentials").First().Should().Be("true");
    }

    [Fact]
    public async Task PreflightOptionsRequest_WithAllowedOrigin_ReturnsCorsAllowHeaders()
    {
        // Arrange
        using var factory = new GatewayWebApplicationFactory();
        using var client = factory.CreateClient();

        using var request = new HttpRequestMessage(HttpMethod.Options, "/v1/events");
        request.Headers.Add("Origin", AllowedOrigin);
        request.Headers.Add("Access-Control-Request-Method", "POST");
        request.Headers.Add("Access-Control-Request-Headers", "Content-Type, Authorization");

        // Act
        using var response = await client.SendAsync(request, TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        response.Headers.Contains("Access-Control-Allow-Origin").Should().BeTrue();
        response.Headers.GetValues("Access-Control-Allow-Origin").First().Should().Be(AllowedOrigin);
        response.Headers.Contains("Access-Control-Allow-Credentials").Should().BeTrue();
    }

    [Fact]
    public async Task Request_WithDisallowedOrigin_DoesNotReturnAllowOriginHeader()
    {
        // Arrange
        using var factory = new GatewayWebApplicationFactory();
        using var client = factory.CreateClient();

        using var request = new HttpRequestMessage(HttpMethod.Get, "/v1/events");
        request.Headers.Add("Origin", DisallowedOrigin);

        // Act
        using var response = await client.SendAsync(request, TestContext.Current.CancellationToken);

        // Assert
        response.Headers.Contains("Access-Control-Allow-Origin").Should().BeFalse();
    }
}
