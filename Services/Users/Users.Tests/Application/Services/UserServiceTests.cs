using System.Security.Cryptography;
using System.Text;
using AwesomeAssertions;
using Moq;
using Microsoft.Extensions.Options;
using Users.Application.Configuration;
using Users.Application.DTOs;
using Users.Application.Exceptions;
using Users.Application.Interfaces.Identity;
using Users.Application.Interfaces.Repositories;
using Users.Application.Services;
using Users.Domain.Entities;
using Users.Domain.Enums;

namespace Users.Tests.Application.Services;

public class UserServiceTests
{
    private readonly Mock<IUserRepository> _userRepositoryMock = new();
    private readonly Mock<IRefreshTokenRepository> _refreshTokenRepositoryMock = new();
    private readonly Mock<ITokenProvider> _tokenProviderMock = new();
    private readonly Mock<IPasswordHasher> _passwordHasherMock = new();

    private readonly UserService _sut;

    public UserServiceTests()
    {
        _sut = new UserService(
            _userRepositoryMock.Object,
            _refreshTokenRepositoryMock.Object,
            _tokenProviderMock.Object,
            _passwordHasherMock.Object,
            Options.Create(new JwtOptions { ExpirationInMinutes = 15, RefreshTokenExpirationInDays = 30 }));
    }

    [Fact]
    public async Task LoginAsync_WithValidCredentials_ShouldReturnAuthResultWithTokens()
    {
        // Arrange
        User user = User.Create("Alice", "hashed_password");
        UserLoginDto loginDto = new("Alice", "secret_pass");

        _userRepositoryMock.Setup(r => r.GetByUserNameAsync("Alice", It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _passwordHasherMock.Setup(h => h.Verify("secret_pass", user.PasswordHash))
            .Returns(true);

        _passwordHasherMock.Setup(h => h.NeedsRehash(user.PasswordHash))
            .Returns(false);

        _tokenProviderMock.Setup(t => t.GenerateToken(It.Is<TokenPayload>(p => p.UserId == user.Id)))
            .Returns("mock-jwt-access-token");

        // Act
        AuthResultDto result = await _sut.LoginAsync(loginDto, TestContext.Current.CancellationToken);

        // Assert
        result.Should().NotBeNull();
        result.AccessToken.Should().Be("mock-jwt-access-token");
        result.RefreshToken.Should().NotBeNullOrWhiteSpace();
        result.User.UserName.Should().Be("Alice");

        _refreshTokenRepositoryMock.Verify(r => r.Add(It.Is<RefreshToken>(t =>
            t.UserId == user.Id &&
            !string.IsNullOrWhiteSpace(t.TokenHash) &&
            t.IsActive)), Times.Once);

        _refreshTokenRepositoryMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task LoginAsync_WithNonExistentUser_ShouldThrowInvalidCredentialsException()
    {
        // Arrange
        UserLoginDto loginDto = new("UnknownUser", "secret_pass");

        _userRepositoryMock.Setup(r => r.GetByUserNameAsync("UnknownUser", It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        // Act
        Func<Task> act = () => _sut.LoginAsync(loginDto, TestContext.Current.CancellationToken);

        // Assert
        await act.Should().ThrowAsync<InvalidCredentialsException>();
    }

    [Fact]
    public async Task LoginAsync_WithInvalidPassword_ShouldThrowInvalidCredentialsException()
    {
        // Arrange
        User user = User.Create("Alice", "hashed_password");
        UserLoginDto loginDto = new("Alice", "wrong_pass");

        _userRepositoryMock.Setup(r => r.GetByUserNameAsync("Alice", It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _passwordHasherMock.Setup(h => h.Verify("wrong_pass", user.PasswordHash))
            .Returns(false);

        // Act
        Func<Task> act = () => _sut.LoginAsync(loginDto, TestContext.Current.CancellationToken);

        // Assert
        await act.Should().ThrowAsync<InvalidCredentialsException>();
    }

    [Fact]
    public async Task LoginAsync_WhenPasswordNeedsRehash_ShouldRehashAndUpdateUser()
    {
        // Arrange
        User user = User.Create("Alice", "old_sha256_hash");
        UserLoginDto loginDto = new("Alice", "correct_pass");

        _userRepositoryMock.Setup(r => r.GetByUserNameAsync("Alice", It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _passwordHasherMock.Setup(h => h.Verify("correct_pass", "old_sha256_hash"))
            .Returns(true);

        _passwordHasherMock.Setup(h => h.NeedsRehash("old_sha256_hash"))
            .Returns(true);

        _passwordHasherMock.Setup(h => h.Hash("correct_pass"))
            .Returns("new_argon2id_hash");

        _tokenProviderMock.Setup(t => t.GenerateToken(It.IsAny<TokenPayload>()))
            .Returns("jwt-token");

        // Act
        AuthResultDto result = await _sut.LoginAsync(loginDto, TestContext.Current.CancellationToken);

        // Assert
        user.PasswordHash.Should().Be("new_argon2id_hash");
        _userRepositoryMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task RefreshSessionAsync_WithValidActiveToken_ShouldRotateAndReturnNewTokens()
    {
        // Arrange
        User user = User.Create("Alice", "hashed_password");
        string rawOldToken = "valid-old-raw-token";
        string oldTokenHash = HashToken(rawOldToken);
        RefreshToken existingToken = RefreshToken.Create(user.Id, oldTokenHash, TimeSpan.FromDays(10));

        _refreshTokenRepositoryMock.Setup(r => r.GetByTokenHashAsync(oldTokenHash, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingToken);

        _userRepositoryMock.Setup(r => r.GetByIdAsync(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _tokenProviderMock.Setup(t => t.GenerateToken(It.Is<TokenPayload>(p => p.UserId == user.Id)))
            .Returns("new-jwt-access-token");

        // Act
        AuthResultDto result = await _sut.RefreshSessionAsync(rawOldToken, TestContext.Current.CancellationToken);

        // Assert
        result.Should().NotBeNull();
        result.AccessToken.Should().Be("new-jwt-access-token");
        result.RefreshToken.Should().NotBeNullOrWhiteSpace();
        result.RefreshToken.Should().NotBe(rawOldToken);

        // Старый токен должен быть отозван и содержать хэш нового
        existingToken.IsRevoked.Should().BeTrue();
        existingToken.ReplacedByTokenHash.Should().NotBeNullOrWhiteSpace();

        // Новый токен должен быть добавлен в репозиторий
        _refreshTokenRepositoryMock.Verify(r => r.Add(It.Is<RefreshToken>(t =>
            t.UserId == user.Id &&
            t.IsActive)), Times.Once);

        _refreshTokenRepositoryMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RefreshSessionAsync_WithNonExistentToken_ShouldThrowInvalidCredentialsException()
    {
        // Arrange
        string rawToken = "non-existent-token";
        string tokenHash = HashToken(rawToken);

        _refreshTokenRepositoryMock.Setup(r => r.GetByTokenHashAsync(tokenHash, It.IsAny<CancellationToken>()))
            .ReturnsAsync((RefreshToken?)null);

        // Act
        Func<Task> act = () => _sut.RefreshSessionAsync(rawToken, TestContext.Current.CancellationToken);

        // Assert
        await act.Should().ThrowAsync<InvalidCredentialsException>();
    }

    [Fact]
    public async Task RefreshSessionAsync_WithExpiredToken_ShouldThrowInvalidCredentialsException()
    {
        // Arrange
        Guid userId = Guid.NewGuid();
        string rawToken = "expired-raw-token";
        string tokenHash = HashToken(rawToken);

        // Создаем токен с миллисекундным временем жизни и ждем истечения
        RefreshToken expiredToken = RefreshToken.Create(userId, tokenHash, TimeSpan.FromMilliseconds(50));
        await Task.Delay(80, TestContext.Current.CancellationToken);

        _refreshTokenRepositoryMock.Setup(r => r.GetByTokenHashAsync(tokenHash, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expiredToken);

        // Act
        Func<Task> act = () => _sut.RefreshSessionAsync(rawToken, TestContext.Current.CancellationToken);

        // Assert
        await act.Should().ThrowAsync<InvalidCredentialsException>();
    }

    [Fact]
    public async Task RefreshSessionAsync_WithRevokedToken_ShouldTriggerTheftDetectionAndRevokeAllUserTokens()
    {
        // Arrange
        Guid userId = Guid.NewGuid();
        string rawCompromisedToken = "stolen-and-already-used-token";
        string tokenHash = HashToken(rawCompromisedToken);

        RefreshToken alreadyRevokedToken = RefreshToken.Create(userId, tokenHash, TimeSpan.FromDays(10));
        alreadyRevokedToken.Revoke(); // Токен уже был отозван ранее!

        _refreshTokenRepositoryMock.Setup(r => r.GetByTokenHashAsync(tokenHash, It.IsAny<CancellationToken>()))
            .ReturnsAsync(alreadyRevokedToken);

        // Act
        Func<Task> act = () => _sut.RefreshSessionAsync(rawCompromisedToken, TestContext.Current.CancellationToken);

        // Assert: Должно выбросить исключение и АННУЛИРОВАТЬ ВСЕ СЕССИИ пользователя
        await act.Should().ThrowAsync<InvalidCredentialsException>();

        _refreshTokenRepositoryMock.Verify(r => r.RevokeAllActiveByUserIdAsync(userId, It.IsAny<CancellationToken>()), Times.Once);
        _refreshTokenRepositoryMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RevokeSessionAsync_WithActiveToken_ShouldRevokeAndSave()
    {
        // Arrange
        Guid userId = Guid.NewGuid();
        string rawToken = "token-to-revoke";
        string tokenHash = HashToken(rawToken);
        RefreshToken activeToken = RefreshToken.Create(userId, tokenHash, TimeSpan.FromDays(10));

        _refreshTokenRepositoryMock.Setup(r => r.GetByTokenHashAsync(tokenHash, It.IsAny<CancellationToken>()))
            .ReturnsAsync(activeToken);

        // Act
        await _sut.RevokeSessionAsync(rawToken, TestContext.Current.CancellationToken);

        // Assert
        activeToken.IsRevoked.Should().BeTrue();
        _refreshTokenRepositoryMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RevokeSessionAsync_WithNonExistentToken_ShouldNotThrow()
    {
        // Arrange
        string rawToken = "non-existent-token";
        string tokenHash = HashToken(rawToken);

        _refreshTokenRepositoryMock.Setup(r => r.GetByTokenHashAsync(tokenHash, It.IsAny<CancellationToken>()))
            .ReturnsAsync((RefreshToken?)null);

        // Act
        Func<Task> act = () => _sut.RevokeSessionAsync(rawToken, TestContext.Current.CancellationToken);

        // Assert
        await act.Should().NotThrowAsync();
        _refreshTokenRepositoryMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RegisterAsync_WithExistingUser_ShouldThrowUserAlreadyExistsException()
    {
        // Arrange
        UserRegisterDto registerDto = new("ExistingUser", "password");
        User existingUser = User.Create("ExistingUser", "hash");

        _userRepositoryMock.Setup(r => r.GetByUserNameAsync("ExistingUser", It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingUser);

        // Act
        Func<Task> act = () => _sut.RegisterAsync(registerDto, TestContext.Current.CancellationToken);

        // Assert
        await act.Should().ThrowAsync<UserAlreadyExistsException>();
    }

    [Fact]
    public async Task RegisterAsync_WithValidData_ShouldAddUserAndSave()
    {
        // Arrange
        UserRegisterDto registerDto = new("NewUser", "password");

        _userRepositoryMock.Setup(r => r.GetByUserNameAsync("NewUser", It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        _passwordHasherMock.Setup(h => h.Hash("password"))
            .Returns("hashed_password");

        // Act
        await _sut.RegisterAsync(registerDto, TestContext.Current.CancellationToken);

        // Assert
        _userRepositoryMock.Verify(r => r.Add(It.Is<User>(u => u.UserName == "NewUser")), Times.Once);
        _userRepositoryMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    private static string HashToken(string token)
    {
        byte[] bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return Convert.ToHexString(bytes);
    }
}
