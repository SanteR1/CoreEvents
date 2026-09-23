using System.Net;
using Gateway.Tests.Fixtures;

namespace Gateway.Tests.Integration;

public class RateLimitingTests
{
    [Fact]
    public async Task AuthRoute_WhenPermitLimitExceeded_Returns429TooManyRequests()
    {
        // Arrange: устанавливаем лимит 3 запроса для быстрого тестирования
        var overrides = new Dictionary<string, string?>
        {
            ["RateLimiting:Auth:PermitLimit"] = "3",
            ["RateLimiting:Auth:WindowSeconds"] = "60",
            ["RateLimiting:Auth:QueueLimit"] = "0"
        };

        using var factory = new GatewayWebApplicationFactory(configurationOverrides: overrides);
        using var client = factory.CreateClient();

        // Act & Assert: первые 3 запроса не должны возвращать 429
        for (int i = 0; i < 3; i++)
        {
            var response = await client.GetAsync("/v1/auth/me", TestContext.Current.CancellationToken);
            response.StatusCode.Should().NotBe(HttpStatusCode.TooManyRequests);
        }

        // 4-й запрос превышает лимит и блокируется Rate Limiter
        var blockedResponse = await client.GetAsync("/v1/auth/me", TestContext.Current.CancellationToken);
        blockedResponse.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
    }

    [Fact]
    public async Task NonAuthRoute_DoesNotUseAuthRateLimitPolicy()
    {
        // Arrange: устанавливаем лимит 2 для auth
        var overrides = new Dictionary<string, string?>
        {
            ["RateLimiting:Auth:PermitLimit"] = "2",
            ["RateLimiting:Auth:WindowSeconds"] = "60",
            ["RateLimiting:Auth:QueueLimit"] = "0"
        };

        using var factory = new GatewayWebApplicationFactory(configurationOverrides: overrides);
        using var client = factory.CreateClient();

        // Act & Assert: отправляем 4 запроса на роут events (без политики AuthRateLimit)
        for (int i = 0; i < 4; i++)
        {
            var response = await client.GetAsync("/v1/events", TestContext.Current.CancellationToken);
            response.StatusCode.Should().NotBe(HttpStatusCode.TooManyRequests);
        }
    }
}
