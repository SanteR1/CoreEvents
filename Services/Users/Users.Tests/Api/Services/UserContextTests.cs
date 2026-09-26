using System.Security.Claims;
using AwesomeAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.IdentityModel.JsonWebTokens;
using Moq;
using Users.Api.Services;
using Users.Domain.Enums;

namespace Users.Tests.Api.Services;

public class UserContextTests
{
    private UserContext CreateContextWithClaims(IEnumerable<Claim>? claims = null, bool isAuthenticated = true)
    {
        DefaultHttpContext httpContext = new();

        if (claims != null)
        {
            // Передача строки "TestAuthType" делает свойство Identity.IsAuthenticated равным true
            string? identityType = isAuthenticated ? "TestAuthType" : null;
            ClaimsIdentity identity = new(claims, identityType);
            httpContext.User = new ClaimsPrincipal(identity);
        }

        Mock<IHttpContextAccessor> accessorMock = new();
        accessorMock.Setup(a => a.HttpContext).Returns(httpContext);

        return new UserContext(accessorMock.Object);
    }

    [Fact]
    public void IsAuthenticated_WithNoIdentity_ShouldReturnFalse()
    {
        // Arrange
        UserContext context = CreateContextWithClaims(null, false);

        // Act
        bool result = context.IsAuthenticated;

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void Role_WithUnknownRoleClaim_ShouldThrowInvalidOperationException()
    {
        // Arrange
        Claim[] claims = new[] { new Claim("role", "SuperHackerRole") };
        UserContext context = CreateContextWithClaims(claims);

        // Act
        Action act = () => _ = context.Role;

        // Assert
        act.Should().Throw<InvalidOperationException>()
           .WithMessage("*Неизвестная роль в токене: SuperHackerRole*");
    }

    [Fact]
    public void Role_WithValidRoleClaim_ShouldReturnRoleEnum()
    {
        // Arrange
        Claim[] claims = new[] { new Claim("role", RoleName.Admin.ToString()) };
        UserContext context = CreateContextWithClaims(claims);

        // Act
        RoleName? result = context.Role;

        // Assert
        result.Should().Be(RoleName.Admin);
    }

    [Fact]
    public void UserId_WithInvalidGuidClaim_ShouldReturnNull()
    {
        // Arrange
        Claim[] claims = new[] { new Claim(JwtRegisteredClaimNames.Sub, "invalid-guid-string") };
        UserContext context = CreateContextWithClaims(claims);

        // Act
        Guid? result = context.UserId;

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void UserId_WithValidGuidClaim_ShouldReturnGuid()
    {
        // Arrange
        Guid expectedId = Guid.NewGuid();
        Claim[] claims = new[] { new Claim(JwtRegisteredClaimNames.Sub, expectedId.ToString()) };
        UserContext context = CreateContextWithClaims(claims);

        // Act
        Guid? result = context.UserId;

        // Assert
        result.Should().Be(expectedId);
    }
}
