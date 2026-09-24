using AwesomeAssertions;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Users.Application.Configuration;
using Users.Application.Interfaces.Identity;
using Users.Domain.Enums;
using Users.Infrastructure.Identity;

namespace Users.Tests.Identity;

public class JwtTokenProviderTests
{
    private readonly JwtOptions _testOptions = new()
    {
        SecretKey = "SuperSecretKeyThatIsAtLeast32BytesLongForSha256!",
        Issuer = "TestIssuer",
        Audience = "TestAudience",
        ExpirationInMinutes = 60
    };

    [Fact]
    public void GenerateToken_WithEmptyEmail_ShouldNotIncludeEmailClaim()
    {
        // Arrange
        IOptions<JwtOptions> options = Options.Create(_testOptions);
        JwtTokenProvider provider = new(options);
        TokenPayload payload = new(Guid.NewGuid(), RoleName.User, string.Empty);

        // Act
        string token = provider.GenerateToken(payload);

        // Assert
        JsonWebTokenHandler handler = new();
        JsonWebToken jwtToken = handler.ReadJsonWebToken(token);

        jwtToken.Claims.Should().NotContain(c => c.Type == JwtRegisteredClaimNames.Email);
    }

    [Fact]
    public void GenerateToken_WithNullPayload_ShouldThrowArgumentNullException()
    {
        // Arrange
        IOptions<JwtOptions> options = Options.Create(_testOptions);
        JwtTokenProvider provider = new(options);

        // Act
        Action act = () => provider.GenerateToken(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void GenerateToken_WithValidPayload_ShouldReturnValidJwtWithClaims()
    {
        // Arrange
        IOptions<JwtOptions> options = Options.Create(_testOptions);
        JwtTokenProvider provider = new(options);
        TokenPayload payload = new(Guid.NewGuid(), RoleName.Admin, "test@example.com");

        // Act
        string token = provider.GenerateToken(payload);

        // Assert
        token.Should().NotBeNullOrWhiteSpace();

        JsonWebTokenHandler handler = new();
        JsonWebToken jwtToken = handler.ReadJsonWebToken(token);

        jwtToken.Subject.Should().Be(payload.UserId.ToString());
        jwtToken.Claims.FirstOrDefault(c => c.Type == "role")?.Value.Should().Be(payload.Role.ToString());

        jwtToken.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Email)?.Value.Should().Be(payload.Email);

        jwtToken.Issuer.Should().Be(_testOptions.Issuer);
        jwtToken.Audiences.Should().Contain(_testOptions.Audience);
    }
}
