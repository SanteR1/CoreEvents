using AwesomeAssertions;
using AwesomeAssertions.Specialized;
using Users.Domain.Entities;
using Users.Domain.Exceptions;

namespace Users.Tests.Domain;

public class RefreshTokenTests
{
    [Fact]
    public void Create_WithValidData_ShouldCreateActiveToken()
    {
        // Arrange
        Guid userId = Guid.NewGuid();
        string tokenHash = "dummy-token-hash-12345";
        TimeSpan lifetime = TimeSpan.FromDays(30);

        // Act
        RefreshToken token = RefreshToken.Create(userId, tokenHash, lifetime);

        // Assert
        token.Should().NotBeNull();
        token.Id.Should().NotBeEmpty();
        token.UserId.Should().Be(userId);
        token.TokenHash.Should().Be(tokenHash);
        token.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
        token.ExpiresAt.Should().BeCloseTo(DateTime.UtcNow.Add(lifetime), TimeSpan.FromSeconds(2));
        token.RevokedAt.Should().BeNull();
        token.ReplacedByTokenHash.Should().BeNull();
        token.IsActive.Should().BeTrue();
        token.IsRevoked.Should().BeFalse();
        token.IsExpired.Should().BeFalse();
    }

    [Fact]
    public void Create_WithEmptyUserId_ShouldThrowValidationException()
    {
        // Arrange
        Guid userId = Guid.Empty;
        string tokenHash = "dummy-token-hash-12345";
        TimeSpan lifetime = TimeSpan.FromDays(30);

        // Act
        Action act = () => RefreshToken.Create(userId, tokenHash, lifetime);

        // Assert
        ExceptionAssertions<ValidationException> exception = act.Should().Throw<ValidationException>();
        exception.Which.ErrorCode.Should().Be("Domain.ValidationFailed");
        exception.Which.ValidationErrors.Should().ContainKey("userId");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithEmptyTokenHash_ShouldThrowValidationException(string emptyHash)
    {
        // Arrange
        Guid userId = Guid.NewGuid();
        TimeSpan lifetime = TimeSpan.FromDays(30);

        // Act
        Action act = () => RefreshToken.Create(userId, emptyHash, lifetime);

        // Assert
        ExceptionAssertions<ValidationException> exception = act.Should().Throw<ValidationException>();
        exception.Which.ErrorCode.Should().Be("Domain.ValidationFailed");
        exception.Which.ValidationErrors.Should().ContainKey("tokenHash");
    }

    [Fact]
    public void Create_WithNonPositiveLifetime_ShouldThrowValidationException()
    {
        // Arrange
        Guid userId = Guid.NewGuid();
        string tokenHash = "dummy-token-hash-12345";

        // Act
        Action act = () => RefreshToken.Create(userId, tokenHash, TimeSpan.Zero);

        // Assert
        ExceptionAssertions<ValidationException> exception = act.Should().Throw<ValidationException>();
        exception.Which.ErrorCode.Should().Be("Domain.ValidationFailed");
        exception.Which.ValidationErrors.Should().ContainKey("lifetime");
    }

    [Fact]
    public void Rotate_WhenActive_ShouldRevokeAndSetReplacedByTokenHash()
    {
        // Arrange
        RefreshToken token = RefreshToken.Create(Guid.NewGuid(), "initial-hash", TimeSpan.FromDays(1));
        string nextHash = "new-token-hash";

        // Act
        token.Rotate(nextHash);

        // Assert
        token.RevokedAt.Should().NotBeNull();
        token.RevokedAt.Value.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
        token.ReplacedByTokenHash.Should().Be(nextHash);
        token.IsRevoked.Should().BeTrue();
        token.IsActive.Should().BeFalse();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Rotate_WithEmptyNewTokenHash_ShouldThrowValidationException(string emptyNextHash)
    {
        // Arrange
        RefreshToken token = RefreshToken.Create(Guid.NewGuid(), "initial-hash", TimeSpan.FromDays(1));

        // Act
        Action act = () => token.Rotate(emptyNextHash);

        // Assert
        ExceptionAssertions<ValidationException> exception = act.Should().Throw<ValidationException>();
        exception.Which.ErrorCode.Should().Be("Domain.ValidationFailed");
        exception.Which.ValidationErrors.Should().ContainKey("newTokenHash");
    }

    [Fact]
    public void Rotate_WhenAlreadyRevoked_ShouldThrowInvalidOperationException()
    {
        // Arrange
        RefreshToken token = RefreshToken.Create(Guid.NewGuid(), "initial-hash", TimeSpan.FromDays(1));
        token.Revoke();

        // Act
        Action act = () => token.Rotate("another-hash");

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Невозможно ротировать неактивный или уже отозванный токен.*");
    }

    [Fact]
    public void Revoke_WhenActive_ShouldSetRevokedAt()
    {
        // Arrange
        RefreshToken token = RefreshToken.Create(Guid.NewGuid(), "initial-hash", TimeSpan.FromDays(1));

        // Act
        token.Revoke();

        // Assert
        token.IsRevoked.Should().BeTrue();
        token.IsActive.Should().BeFalse();
        token.RevokedAt.Should().NotBeNull();
        token.RevokedAt.Value.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
    }

    [Fact]
    public void Revoke_WhenAlreadyRevoked_ShouldBeIdempotent()
    {
        // Arrange
        RefreshToken token = RefreshToken.Create(Guid.NewGuid(), "initial-hash", TimeSpan.FromDays(1));
        token.Revoke();
        DateTime? originalRevokedAt = token.RevokedAt;

        // Act
        token.Revoke();

        // Assert
        token.RevokedAt.Should().Be(originalRevokedAt);
    }
}
