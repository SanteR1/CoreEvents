namespace Users.Api.Services;

public class AuthCookieService : IAuthCookieService
{
    public const string AccessTokenCookieName = "access_token";
    public const string RefreshTokenCookieName = "refresh_token";

    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IWebHostEnvironment _environment;

    public AuthCookieService(IHttpContextAccessor httpContextAccessor,
                             IWebHostEnvironment environment)
    {
        _httpContextAccessor = httpContextAccessor;
        _environment = environment;
    }

    private HttpContext HttpContext => _httpContextAccessor.HttpContext
                                       ?? throw new InvalidOperationException("HttpContext недоступен.");

    public void SetAuthCookies(string accessToken, string refreshToken)
    {
        var response = HttpContext.Response;

        // В Production кука ВСЕГДА Secure (только HTTPS). В Dev — зависит от протокола запроса
        var isSecure = _environment.IsProduction() || HttpContext.Request.IsHttps;

        var versionPrefix = GetVersionPrefix();
        var refreshPath = $"/api/{versionPrefix}/auth";

        // Access Token (15 минут, передается на все эндпоинты через Gateway)
        response.Cookies.Append(AccessTokenCookieName, accessToken, new CookieOptions
        {
            HttpOnly = true,
            Secure = isSecure,
            SameSite = SameSiteMode.Lax,
            Path = "/",
            Expires = DateTimeOffset.UtcNow.AddMinutes(15)
        });

        // Refresh Token (30 дней, изолирован маршрутом авторизации)
        response.Cookies.Append(RefreshTokenCookieName, refreshToken, new CookieOptions
        {
            HttpOnly = true,
            Secure = isSecure,
            SameSite = SameSiteMode.Lax,
            Path = refreshPath,
            Expires = DateTimeOffset.UtcNow.AddDays(30)
        });
    }

    public void ClearAuthCookies()
    {
        var response = HttpContext.Response;
        var isSecure = _environment.IsProduction() || HttpContext.Request.IsHttps;

        var versionPrefix = GetVersionPrefix();
        var refreshPath = $"/api/{versionPrefix}/auth";

        response.Cookies.Delete(AccessTokenCookieName, new CookieOptions { Path = "/", Secure = isSecure });
        response.Cookies.Delete(RefreshTokenCookieName, new CookieOptions { Path = refreshPath, Secure = isSecure });
    }

    public string? GetRefreshToken()
    {
        return HttpContext.Request.Cookies.TryGetValue(RefreshTokenCookieName, out var token)
            ? token
            : null;
    }

    private string GetVersionPrefix()
    {
        var context = _httpContextAccessor.HttpContext;

        // Достаем параметр {version} из маршрута текущего запроса (например, "1", "1.0" или "2")
        var routeVersion = context?.Request.RouteValues["version"]?.ToString();
        if (!string.IsNullOrWhiteSpace(routeVersion))
        {
            var major = routeVersion.Split('.')[0].TrimStart('v', 'V');
            return $"v{major}";
        }
        return "v1";
    }
}
