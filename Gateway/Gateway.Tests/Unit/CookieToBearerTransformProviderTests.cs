using System.Net.Http.Headers;
using Gateway.Api.Transforms;
using Microsoft.AspNetCore.Http;
using Yarp.ReverseProxy.Transforms;
using Yarp.ReverseProxy.Transforms.Builder;

namespace Gateway.Tests.Unit;

public class CookieToBearerTransformProviderTests
{
    private readonly CookieToBearerTransformProvider _provider = new();

    [Fact]
    public async Task Apply_WhenAccessTokenCookiePresent_SetsBearerAuthorizationHeader()
    {
        // Arrange
        var builderContext = new TransformBuilderContext();
        _provider.Apply(builderContext);

        var transform = builderContext.RequestTransforms.Single();

        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers.Append("Cookie", "access_token=test-jwt-token");

        var proxyRequest = new HttpRequestMessage();
        var transformContext = new RequestTransformContext
        {
            HttpContext = httpContext,
            ProxyRequest = proxyRequest
        };

        // Act
        await transform.ApplyAsync(transformContext);

        // Assert
        proxyRequest.Headers.Authorization.Should().NotBeNull();
        proxyRequest.Headers.Authorization!.Scheme.Should().Be("Bearer");
        proxyRequest.Headers.Authorization.Parameter.Should().Be("test-jwt-token");
    }

    [Fact]
    public async Task Apply_WhenAuthorizationHeaderAlreadyPresent_DoesNotOverwriteWithCookie()
    {
        // Arrange
        var builderContext = new TransformBuilderContext();
        _provider.Apply(builderContext);

        var transform = builderContext.RequestTransforms.Single();

        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers.Append("Authorization", "Bearer existing-token");
        httpContext.Request.Headers.Append("Cookie", "access_token=cookie-token");

        var proxyRequest = new HttpRequestMessage();
        proxyRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "existing-token");

        var transformContext = new RequestTransformContext
        {
            HttpContext = httpContext,
            ProxyRequest = proxyRequest
        };

        // Act
        await transform.ApplyAsync(transformContext);

        // Assert
        proxyRequest.Headers.Authorization.Should().NotBeNull();
        proxyRequest.Headers.Authorization!.Parameter.Should().Be("existing-token");
    }

    [Fact]
    public async Task Apply_WhenNoAccessTokenCookie_DoesNotSetAuthorizationHeader()
    {
        // Arrange
        var builderContext = new TransformBuilderContext();
        _provider.Apply(builderContext);

        var transform = builderContext.RequestTransforms.Single();

        var httpContext = new DefaultHttpContext();
        var proxyRequest = new HttpRequestMessage();

        var transformContext = new RequestTransformContext
        {
            HttpContext = httpContext,
            ProxyRequest = proxyRequest
        };

        // Act
        await transform.ApplyAsync(transformContext);

        // Assert
        proxyRequest.Headers.Authorization.Should().BeNull();
    }

    [Fact]
    public async Task Apply_WhenAccessTokenCookieIsEmpty_DoesNotSetAuthorizationHeader()
    {
        // Arrange
        var builderContext = new TransformBuilderContext();
        _provider.Apply(builderContext);

        var transform = builderContext.RequestTransforms.Single();

        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers.Append("Cookie", "access_token=");

        var proxyRequest = new HttpRequestMessage();

        var transformContext = new RequestTransformContext
        {
            HttpContext = httpContext,
            ProxyRequest = proxyRequest
        };

        // Act
        await transform.ApplyAsync(transformContext);

        // Assert
        proxyRequest.Headers.Authorization.Should().BeNull();
    }
}
