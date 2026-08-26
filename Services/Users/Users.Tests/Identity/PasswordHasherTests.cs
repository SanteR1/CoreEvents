using AwesomeAssertions;
using AwesomeAssertions.Specialized;
using Users.Infrastructure.Identity;

namespace Users.Tests.Identity;

public class PasswordHasherTests
{
    [Fact]
    public void Hash_WithEmptyPassword_ShouldThrowArgumentException()
    {
        // Arrange
        string userPassword = "";
        Sha256PasswordHasher hasher = new();

        // Act
        Action act = () => hasher.Hash(userPassword);

        // Assert
        ExceptionAssertions<ArgumentException> exception = act.Should().Throw<ArgumentException>();
        exception.Which.ParamName.Should().Contain("password");
    }

    [Fact]
    public void Hash_WithValidData_ShouldReturnDeterministicString()
    {
        // Arrange
        string userPassword = "Password123";
        Sha256PasswordHasher hasher = new();

        // Act
        string hashPassword1 = hasher.Hash(userPassword);
        string hashPassword2 = hasher.Hash(userPassword);

        // Assert
        hashPassword1.Should().NotBeNullOrWhiteSpace();
        hashPassword1.Should().Be(hashPassword2, "хэш без соли должен быть детерминированным");
    }

    [Fact]
    public void Verify_WithEmptyHash_ShouldThrowArgumentException()
    {
        // Arrange
        string userPassword = "Password!123";
        string hashPassword = "";

        Sha256PasswordHasher hasher = new();

        // Act
        Action act = () => hasher.Verify(userPassword, hashPassword);

        // Assert
        ExceptionAssertions<ArgumentException> exception = act.Should().Throw<ArgumentException>();
        exception.Which.ParamName.Should().Contain("hash");
    }

    [Fact]
    public void Verify_WithEmptyPassword_ShouldThrowArgumentException()
    {
        // Arrange
        string userPassword = "";
        string hashPassword = "hashedPassword";

        Sha256PasswordHasher hasher = new();

        // Act
        Action act = () => hasher.Verify(userPassword, hashPassword);

        // Assert
        ExceptionAssertions<ArgumentException> exception = act.Should().Throw<ArgumentException>();
        exception.Which.ParamName.Should().Contain("password");
    }

    [Fact]
    public void Verify_WithInvalidPassword_ShouldReturnFalse()
    {
        // Arrange
        string userPassword = "Password123";
        string wrongPassword = "WrongPassword123";
        Sha256PasswordHasher hasher = new();
        string hashPassword = hasher.Hash(userPassword);

        // Act
        bool isVerified = hasher.Verify(wrongPassword, hashPassword);

        // Assert
        isVerified.Should().BeFalse();
    }

    [Fact]
    public void Verify_WithValidData_ShouldReturnTrue()
    {
        // Arrange
        string userPassword = "Password123";
        Sha256PasswordHasher hasher = new();
        string hashPassword = hasher.Hash(userPassword);

        // Act
        bool isVerified = hasher.Verify(userPassword, hashPassword);

        // Assert
        isVerified.Should().BeTrue();
    }
}
