using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;

namespace Bookings.IntegrationTests.Infrastructure.Auth;

public class TestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public const string AuthenticationScheme = "TestScheme";

    public TestAuthHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder) : base(options, logger, encoder)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var role = "User";
        if (Context.Request.Headers.TryGetValue("X-Test-Role", out var testRole))
        {
            role = testRole.ToString();
        }

        Guid id = Guid.NewGuid();
        if (Context.Request.Headers.TryGetValue("X-Test-Guid", out var testId))
        {
            if (Guid.TryParse(testId, out var testUserId))
            {
                id = testUserId;
            }
        }

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
            new Claim(ClaimTypes.Name, "TestUser"),
            //new Claim(ClaimTypes.Role, role),
            new Claim("role", role),
            new Claim(JwtRegisteredClaimNames.Sub, id.ToString())
        };

        var identity = new ClaimsIdentity(claims, AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, AuthenticationScheme);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
