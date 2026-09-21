using System.Security.Cryptography;
using System.Text;
using AwesomeAssertions;
using AwesomeAssertions.Specialized;
using Users.Infrastructure.Identity;

namespace Users.Tests.Identity;

public class PasswordHasherTests
{
    private readonly Argon2idPasswordHasher _hasher = new();

    [Fact]
    public void Hash_WithEmptyPassword_ShouldThrowArgumentException()
    {
        // Arrange
        string userPassword = "";

        // Act
        Action act = () => _hasher.Hash(userPassword);

        // Assert
        ExceptionAssertions<ArgumentException> exception = act.Should().Throw<ArgumentException>();
        exception.Which.ParamName.Should().Contain("password");
    }

    [Fact]
    public void Hash_WithValidData_ShouldReturnSaltedNonDeterministicHash()
    {
        // Arrange
        string userPassword = "Password123";

        // Act
        string hashPassword1 = _hasher.Hash(userPassword);
        string hashPassword2 = _hasher.Hash(userPassword);

        // Assert
        hashPassword1.Should().NotBeNullOrWhiteSpace();
        hashPassword2.Should().NotBeNullOrWhiteSpace();
        hashPassword1.Should().NotBe(hashPassword2, "каждый вызов должен генерировать уникальную соль");

        _hasher.Verify(userPassword, hashPassword1).Should().BeTrue();
        _hasher.Verify(userPassword, hashPassword2).Should().BeTrue();
    }

    [Fact]
    public void Verify_WithEmptyHash_ShouldThrowArgumentException()
    {
        // Arrange
        string userPassword = "Password!123";
        
        // Act
        Action act = () => _hasher.Verify(userPassword, "");

        // Assert
        ExceptionAssertions<ArgumentException> exception = act.Should().Throw<ArgumentException>();
        exception.Which.ParamName.Should().Contain("hash");
    }

    [Fact]
    public void Verify_WithEmptyPassword_ShouldThrowArgumentException()
    {
        // Arrange
        string hashPassword = "hashedPassword";

        // Act
        Action act = () => _hasher.Verify("", hashPassword);

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
        string hashPassword = _hasher.Hash(userPassword);

        // Act
        bool isVerified = _hasher.Verify(wrongPassword, hashPassword);

        // Assert
        isVerified.Should().BeFalse();
    }

    [Fact]
    public void Verify_WithValidData_ShouldReturnTrue()
    {
        // Arrange
        string userPassword = "Password123";
        string hashPassword = _hasher.Hash(userPassword);

        // Act
        bool isVerified = _hasher.Verify(userPassword, hashPassword);

        // Assert
        isVerified.Should().BeTrue();
    }

    [Fact]
    public void Verify_WithLegacySha256Hash_ShouldReturnTrue()
    {
        // Arrange
        string password = "LegacyPassword123";
        string legacySha256Hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(password)));

        // Act
        bool isVerified = _hasher.Verify(password, legacySha256Hash);

        // Assert
        isVerified.Should().BeTrue("новый хешер должен уметь проверять старые SHA-256 хеши");
    }
    [Fact]
    public void NeedsRehash_WithLegacySha256Hash_ShouldReturnTrue()
    {
        // Arrange
        string legacySha256Hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes("Password123")));

        // Act
        bool needsRehash = _hasher.NeedsRehash(legacySha256Hash);

        // Assert
        needsRehash.Should().BeTrue("старый SHA-256 хеш всегда должен требовать обновления");
    }
    [Fact]
    public void NeedsRehash_WithCurrentArgon2idHash_ShouldReturnFalse()
    {
        // Arrange
        string currentHash = _hasher.Hash("Password123");

        // Act
        bool needsRehash = _hasher.NeedsRehash(currentHash);

        // Assert
        needsRehash.Should().BeFalse("актуальный Argon2id хеш не требует обновления хеша");
    }
}
