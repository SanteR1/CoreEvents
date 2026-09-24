using System.Net;
using Gateway.Tests.Fixtures;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;

namespace Gateway.Tests.Integration;

public class BffReverseProxyIntegrationTests
{
    [Fact]
    public async Task Proxy_WithAccessTokenCookie_TransformsToBearerAuthorizationHeaderOnDownstream()
    {
        // 1. Поднимаем тестовый downstream-сервер на случайном свободном порту (порт 0)
        var downstreamBuilder = WebApplication.CreateBuilder();
        downstreamBuilder.WebHost.UseUrls("http://127.0.0.1:0");
        var downstream = downstreamBuilder.Build();

        string? capturedAuthHeader = null;
        downstream.MapGet("/v1/events", (HttpRequest req) =>
        {
            capturedAuthHeader = req.Headers.Authorization.ToString();
            return Results.Ok(new { message = "downstream response" });
        });

        await downstream.StartAsync(TestContext.Current.CancellationToken);

        try
        {
            var downstreamUrl = downstream.Urls.First();

            // 2. Настраиваем Gateway на адрес поднятого тестового downstream
            var overrides = new Dictionary<string, string?>
            {
                ["ReverseProxy:Clusters:events-cluster:Destinations:destination1:Address"] = downstreamUrl
            };

            using var factory = new GatewayWebApplicationFactory(configurationOverrides: overrides);
            using var client = factory.CreateClient();

            using var request = new HttpRequestMessage(HttpMethod.Get, "/v1/events");
            request.Headers.Add("Cookie", "access_token=super-secure-jwt-token");

            // 3. Отправляем запрос через шлюз
            using var response = await client.SendAsync(request, TestContext.Current.CancellationToken);

            // 4. Проверяем, что запрос успешно дошел и заголовок Bearer был добавлен
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            capturedAuthHeader.Should().Be("Bearer super-secure-jwt-token");
        }
        finally
        {
            await downstream.StopAsync(TestContext.Current.CancellationToken);
            await downstream.DisposeAsync();
        }
    }

    [Fact]
    public async Task Proxy_WithExistingAuthorizationHeader_PreservesOriginalHeader()
    {
        // 1. Поднимаем тестовый downstream-сервер на случайном порту
        var downstreamBuilder = WebApplication.CreateBuilder();
        downstreamBuilder.WebHost.UseUrls("http://127.0.0.1:0");
        var downstream = downstreamBuilder.Build();

        string? capturedAuthHeader = null;
        downstream.MapGet("/v1/events", (HttpRequest req) =>
        {
            capturedAuthHeader = req.Headers.Authorization.ToString();
            return Results.Ok(new { message = "downstream response" });
        });

        await downstream.StartAsync(TestContext.Current.CancellationToken);

        try
        {
            var downstreamUrl = downstream.Urls.First();

            var overrides = new Dictionary<string, string?>
            {
                ["ReverseProxy:Clusters:events-cluster:Destinations:destination1:Address"] = downstreamUrl
            };

            using var factory = new GatewayWebApplicationFactory(configurationOverrides: overrides);
            using var client = factory.CreateClient();

            using var request = new HttpRequestMessage(HttpMethod.Get, "/v1/events");
            request.Headers.Add("Authorization", "Bearer existing-bearer-token");
            request.Headers.Add("Cookie", "access_token=ignored-cookie-token");

            using var response = await client.SendAsync(request, TestContext.Current.CancellationToken);

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            capturedAuthHeader.Should().Be("Bearer existing-bearer-token");
        }
        finally
        {
            await downstream.StopAsync(TestContext.Current.CancellationToken);
            await downstream.DisposeAsync();
        }
    }
}
