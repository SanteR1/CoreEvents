using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Users.Application.Interfaces.Identity;

namespace Users.Infrastructure.Identity;

public class JwtTokenProvider(IOptions<JwtOptions> options) : ITokenProvider
{
    private readonly JwtOptions _options = options.Value;

    public string GenerateToken(TokenPayload payload)
    {
        ArgumentNullException.ThrowIfNull(payload);

        var claims = new Dictionary<string, object>
        {
            [JwtRegisteredClaimNames.Sub] = payload.UserId.ToString(),
            ["role"] = payload.Role.ToString(),
            [JwtRegisteredClaimNames.Jti] = Guid.NewGuid().ToString()
        };
        if (!string.IsNullOrWhiteSpace(payload.Email))
        {
            claims[JwtRegisteredClaimNames.Email] = payload.Email;
        }

        var signingKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(_options.SecretKey));
        var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = _options.Issuer,
            Audience = _options.Audience,
            Claims = claims,
            NotBefore = DateTime.UtcNow,
            Expires = DateTime.UtcNow.AddMinutes(_options.ExpirationInMinutes),
            IssuedAt = DateTime.UtcNow,
            SigningCredentials = credentials
        };

        return new JsonWebTokenHandler().CreateToken(descriptor);
    }
}
