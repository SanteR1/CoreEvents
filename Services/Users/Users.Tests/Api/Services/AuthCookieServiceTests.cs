using AwesomeAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Primitives;
using Moq;
using Users.Api.Services;

namespace Users.Tests.Api.Services;

public class AuthCookieServiceTests
{
    private readonly Mock<IHttpContextAccessor> _httpContextAccessorMock = new();
    private readonly Mock<IWebHostEnvironment> _environmentMock = new();

    private AuthCookieService CreateSut(HttpContext httpContext, string environment = "Development")
    {
        _httpContextAccessorMock.Setup(a => a.HttpContext).Returns(httpContext);
        _environmentMock.Setup(e => e.EnvironmentName).Returns(environment);

        return new AuthCookieService(_httpContextAccessorMock.Object, _environmentMock.Object);
    }

    [Fact]
    public void SetAuthCookies_InDevelopmentOverHttp_ShouldSetCookiesWithSecureFalseAndDefaultV1Path()
    {
        // Arrange
        DefaultHttpContext httpContext = new();
        httpContext.Request.IsHttps = false;

        AuthCookieService sut = CreateSut(httpContext, "Development");

        // Act
        sut.SetAuthCookies("test-access-token", "test-refresh-token");

        // Assert
        StringValues setCookies = httpContext.Response.Headers.SetCookie;
        setCookies.Should().HaveCount(2);

        string? accessTokenHeader = setCookies.FirstOrDefault(c => c != null && c.StartsWith("access_token="));
        accessTokenHeader.Should().NotBeNull();
        accessTokenHeader.Should().Contain("access_token=test-access-token");
        accessTokenHeader.Should().Contain("path=/");
        accessTokenHeader.Should().Contain("httponly");
        accessTokenHeader.Should().Contain("samesite=lax");
        accessTokenHeader.Should().NotContain("secure");

        string? refreshTokenHeader = setCookies.FirstOrDefault(c => c != null && c.StartsWith("refresh_token="));
        refreshTokenHeader.Should().NotBeNull();
        refreshTokenHeader.Should().Contain("refresh_token=test-refresh-token");
        refreshTokenHeader.Should().Contain("path=/api/v1/auth");
        refreshTokenHeader.Should().Contain("httponly");
        refreshTokenHeader.Should().Contain("samesite=lax");
        refreshTokenHeader.Should().NotContain("secure");
    }

    [Fact]
    public void SetAuthCookies_InDevelopmentOverHttps_ShouldSetCookiesWithSecureTrue()
    {
        // Arrange
        DefaultHttpContext httpContext = new();
        httpContext.Request.IsHttps = true;

        AuthCookieService sut = CreateSut(httpContext, "Development");

        // Act
        sut.SetAuthCookies("test-access-token", "test-refresh-token");

        // Assert
        StringValues setCookies = httpContext.Response.Headers.SetCookie;
        setCookies.Should().HaveCount(2);

        string? accessTokenHeader = setCookies.FirstOrDefault(c => c != null && c.StartsWith("access_token="));
        accessTokenHeader.Should().NotBeNull();
        accessTokenHeader.Should().Contain("secure");

        string? refreshTokenHeader = setCookies.FirstOrDefault(c => c != null && c.StartsWith("refresh_token="));
        refreshTokenHeader.Should().NotBeNull();
        refreshTokenHeader.Should().Contain("secure");
    }

    [Fact]
    public void SetAuthCookies_InProduction_ShouldAlwaysSetCookiesWithSecureTrueEvenOverHttp()
    {
        // Arrange
        DefaultHttpContext httpContext = new();
        httpContext.Request.IsHttps = false; // Запрос пришел от reverse proxy по HTTP

        AuthCookieService sut = CreateSut(httpContext, "Production");

        // Act
        sut.SetAuthCookies("test-access-token", "test-refresh-token");

        // Assert
        StringValues setCookies = httpContext.Response.Headers.SetCookie;
        setCookies.Should().HaveCount(2);

        string? accessTokenHeader = setCookies.FirstOrDefault(c => c != null && c.StartsWith("access_token="));
        accessTokenHeader.Should().NotBeNull();
        accessTokenHeader.Should().Contain("secure");

        string? refreshTokenHeader = setCookies.FirstOrDefault(c => c != null && c.StartsWith("refresh_token="));
        refreshTokenHeader.Should().NotBeNull();
        refreshTokenHeader.Should().Contain("secure");
    }

    [Fact]
    public void SetAuthCookies_WithRouteVersion_ShouldScopeRefreshTokenPathToSpecifiedVersion()
    {
        // Arrange
        DefaultHttpContext httpContext = new();
        httpContext.Request.RouteValues = new RouteValueDictionary
        {
            ["version"] = "2.0"
        };

        AuthCookieService sut = CreateSut(httpContext, "Development");

        // Act
        sut.SetAuthCookies("test-access-token", "test-refresh-token");

        // Assert
        StringValues setCookies = httpContext.Response.Headers.SetCookie;
        string? refreshTokenHeader = setCookies.FirstOrDefault(c => c != null && c.StartsWith("refresh_token="));

        refreshTokenHeader.Should().NotBeNull();
        refreshTokenHeader.Should().Contain("path=/api/v2/auth");
    }

    [Fact]
    public void GetRefreshToken_WhenCookieExists_ShouldReturnToken()
    {
        // Arrange
        DefaultHttpContext httpContext = new();
        httpContext.Request.Headers.Cookie = "refresh_token=sample-refresh-token-12345";

        AuthCookieService sut = CreateSut(httpContext);

        // Act
        string? result = sut.GetRefreshToken();

        // Assert
        result.Should().Be("sample-refresh-token-12345");
    }

    [Fact]
    public void GetRefreshToken_WhenCookieDoesNotExist_ShouldReturnNull()
    {
        // Arrange
        DefaultHttpContext httpContext = new();

        AuthCookieService sut = CreateSut(httpContext);

        // Act
        string? result = sut.GetRefreshToken();

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void ClearAuthCookies_ShouldDeleteBothCookiesWithMatchingPaths()
    {
        // Arrange
        DefaultHttpContext httpContext = new();
        httpContext.Request.RouteValues = new RouteValueDictionary
        {
            ["version"] = "1"
        };

        AuthCookieService sut = CreateSut(httpContext);

        // Act
        sut.ClearAuthCookies();

        // Assert
        StringValues setCookies = httpContext.Response.Headers.SetCookie;
        setCookies.Should().HaveCount(2);

        string? accessCookie = setCookies.FirstOrDefault(c => c != null && c.StartsWith("access_token="));
        accessCookie.Should().NotBeNull();
        accessCookie.Should().Contain("path=/");
        accessCookie.Should().Contain("expires=Thu, 01 Jan 1970 00:00:00 GMT");

        string? refreshCookie = setCookies.FirstOrDefault(c => c != null && c.StartsWith("refresh_token="));
        refreshCookie.Should().NotBeNull();
        refreshCookie.Should().Contain("path=/api/v1/auth");
        refreshCookie.Should().Contain("expires=Thu, 01 Jan 1970 00:00:00 GMT");
    }

    [Fact]
    public void ClearAuthCookies_InProduction_ShouldDeleteCookiesWithSecureTrue()
    {
        // Arrange
        DefaultHttpContext httpContext = new();
        httpContext.Request.RouteValues = new RouteValueDictionary
        {
            ["version"] = "1"
        };

        AuthCookieService sut = CreateSut(httpContext, "Production");

        // Act
        sut.ClearAuthCookies();

        // Assert
        StringValues setCookies = httpContext.Response.Headers.SetCookie;
        setCookies.Should().HaveCount(2);

        string? accessCookie = setCookies.FirstOrDefault(c => c != null && c.StartsWith("access_token="));
        accessCookie.Should().NotBeNull();
        accessCookie.Should().Contain("secure");

        string? refreshCookie = setCookies.FirstOrDefault(c => c != null && c.StartsWith("refresh_token="));
        refreshCookie.Should().NotBeNull();
        refreshCookie.Should().Contain("secure");
    }

    [Fact]
    public void SetAuthCookies_WhenHttpContextIsNull_ShouldThrowInvalidOperationException()
    {
        // Arrange
        _httpContextAccessorMock.Setup(a => a.HttpContext).Returns((HttpContext?)null);
        AuthCookieService sut = new(_httpContextAccessorMock.Object, _environmentMock.Object);

        // Act
        Action act = () => sut.SetAuthCookies("token1", "token2");

        // Assert
        act.Should().Throw<InvalidOperationException>()
           .WithMessage("*HttpContext недоступен.*");
    }
}
