using System.Security.Cryptography;
using System.Text;
using AwesomeAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Users.Application.DTOs;
using Users.Application.Exceptions;
using Users.Application.Interfaces.Services;
using Users.Domain.Entities;
using Users.Domain.Enums;
using Users.IntegrationTests.Infrastructure.Bases;
using Users.IntegrationTests.Infrastructure.Factories;

namespace Users.IntegrationTests.Services;

public class UserServiceTests(ApiOnlyIntegrationTestFactory factory) : ApiOnlyIntegrationTestBase(factory)
{
    [Fact]
    public async Task LoginAsync_WithInvalidPassword_ThrowsInvalidCredentialsException()
    {
        // Arrange
        UserRegisterDto registerRequest = new("UserName", "Password123");

        await ExecuteScopeAsync(sp =>
        {
            IAuthService authService = sp.GetRequiredService<IAuthService>();

            return authService.RegisterAsync(registerRequest, TestContext.Current.CancellationToken);
        });

        UserLoginDto loginDto = new("UserName", "123Password");

        // Act & Assert
        await Assert.ThrowsAsync<InvalidCredentialsException>(() =>
            ExecuteScopeAsync(sp =>
            {
                IAuthService authService = sp.GetRequiredService<IAuthService>();

                return authService.LoginAsync(loginDto, TestContext.Current.CancellationToken);
            }));
    }

    [Fact]
    public async Task LoginAsync_WithValidCredentials_ReturnsTokensAndSavesRefreshTokenToDb()
    {
        // Arrange
        UserRegisterDto registerRequest = new("UserName", "Password123");

        await ExecuteScopeAsync(sp =>
        {
            IAuthService authService = sp.GetRequiredService<IAuthService>();

            return authService.RegisterAsync(registerRequest, TestContext.Current.CancellationToken);
        });

        UserLoginDto loginDto = new(registerRequest.UserName, registerRequest.Password);

        // Act
        AuthResultDto result = await ExecuteScopeAsync(sp =>
        {
            IAuthService authService = sp.GetRequiredService<IAuthService>();

            return authService.LoginAsync(loginDto, TestContext.Current.CancellationToken);
        });

        // Assert
        result.Should().NotBeNull();
        result.AccessToken.Should().NotBeNullOrWhiteSpace();
        result.RefreshToken.Should().NotBeNullOrWhiteSpace();
        result.User.UserName.Should().Be(loginDto.UserName);

        // Проверяем наличие токена в БД
        var tokenHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(result.RefreshToken)));
        await ExecuteDbContextAsync(async db =>
        {
            RefreshToken? dbToken = await db.RefreshTokens.FirstOrDefaultAsync(t => t.TokenHash == tokenHash);
            dbToken.Should().NotBeNull();
            dbToken.UserId.Should().Be(result.User.Id);
            dbToken.IsActive.Should().BeTrue();
        });
    }

    [Fact]
    public async Task RegisterAsync_WithExistingUserName_ThrowsUserAlreadyExistsException()
    {
        // Arrange
        UserRegisterDto request = new("UserName", "Password123");

        await ExecuteScopeAsync(sp =>
        {
            IAuthService authService = sp.GetRequiredService<IAuthService>();

            return authService.RegisterAsync(request, TestContext.Current.CancellationToken);
        });

        // Act & Assert
        await Assert.ThrowsAsync<UserAlreadyExistsException>(() =>
            ExecuteScopeAsync(sp =>
            {
                IAuthService authService = sp.GetRequiredService<IAuthService>();

                return authService.RegisterAsync(request, TestContext.Current.CancellationToken);
            }));
    }

    [Fact]
    public async Task RegisterAsync_WithValidData_CreatesUserWithUserRole()
    {
        // Arrange
        UserRegisterDto request = new("UserName", "Password123");

        // Act
        await ExecuteScopeAsync(sp =>
        {
            IAuthService authService = sp.GetRequiredService<IAuthService>();

            return authService.RegisterAsync(request, TestContext.Current.CancellationToken);
        });

        // Assert
        await ExecuteDbContextAsync(async db =>
        {
            User? savedUser = await db.Users.FirstOrDefaultAsync(u => u.UserName == request.UserName);
            savedUser.Should().NotBeNull();
            savedUser.UserName.Should().Be(request.UserName);
            savedUser.Role.Should().Be(RoleName.User);
        });
    }

    [Fact]
    public async Task RefreshSessionAsync_WithValidToken_RotatesTokenInDatabase()
    {
        // Arrange
        UserRegisterDto registerRequest = new("Bob", "Password123");
        await ExecuteScopeAsync(sp => sp.GetRequiredService<IAuthService>().RegisterAsync(registerRequest, TestContext.Current.CancellationToken));

        AuthResultDto loginResult = await ExecuteScopeAsync(sp =>
            sp.GetRequiredService<IAuthService>().LoginAsync(new UserLoginDto("Bob", "Password123"), TestContext.Current.CancellationToken));

        // Act
        AuthResultDto refreshResult = await ExecuteScopeAsync(sp =>
            sp.GetRequiredService<IAuthService>().RefreshSessionAsync(loginResult.RefreshToken, TestContext.Current.CancellationToken));

        // Assert
        refreshResult.Should().NotBeNull();
        refreshResult.AccessToken.Should().NotBeNullOrWhiteSpace();
        refreshResult.RefreshToken.Should().NotBe(loginResult.RefreshToken);

        var oldHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(loginResult.RefreshToken)));
        var newHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(refreshResult.RefreshToken)));

        await ExecuteDbContextAsync(async db =>
        {
            RefreshToken? oldToken = await db.RefreshTokens.FirstOrDefaultAsync(t => t.TokenHash == oldHash);
            oldToken.Should().NotBeNull();
            oldToken.IsRevoked.Should().BeTrue();
            oldToken.ReplacedByTokenHash.Should().Be(newHash);

            RefreshToken? newToken = await db.RefreshTokens.FirstOrDefaultAsync(t => t.TokenHash == newHash);
            newToken.Should().NotBeNull();
            newToken.IsActive.Should().BeTrue();
        });
    }

    [Fact]
    public async Task RefreshSessionAsync_WithCompromisedRevokedToken_RevokesAllSessionsInDatabase()
    {
        // Arrange
        UserRegisterDto registerRequest = new("Charlie", "Password123");
        await ExecuteScopeAsync(sp => sp.GetRequiredService<IAuthService>().RegisterAsync(registerRequest, TestContext.Current.CancellationToken));

        AuthResultDto loginResult = await ExecuteScopeAsync(sp =>
            sp.GetRequiredService<IAuthService>().LoginAsync(new UserLoginDto("Charlie", "Password123"), TestContext.Current.CancellationToken));

        // Шаг 1: Первая корректная ротация (старый токен становится отозванным)
        AuthResultDto refreshResult = await ExecuteScopeAsync(sp =>
            sp.GetRequiredService<IAuthService>().RefreshSessionAsync(loginResult.RefreshToken, TestContext.Current.CancellationToken));

        // Шаг 2: Хакер пытается повторно использовать loginResult.RefreshToken (Reuse Detection)
        await Assert.ThrowsAsync<InvalidCredentialsException>(() =>
            ExecuteScopeAsync(sp => sp.GetRequiredService<IAuthService>().RefreshSessionAsync(loginResult.RefreshToken, TestContext.Current.CancellationToken)));

        // Проверяем, что ВСЕ токены пользователя в БД теперь отозваны
        await ExecuteDbContextAsync(async db =>
        {
            var userTokens = await db.RefreshTokens.Where(t => t.UserId == loginResult.User.Id).ToListAsync();
            userTokens.Should().NotBeEmpty();
            userTokens.Should().OnlyContain(t => t.RevokedAt != null);
        });
    }

    [Fact]
    public async Task RevokeSessionAsync_WithActiveToken_MarksTokenRevokedInDatabase()
    {
        // Arrange
        UserRegisterDto registerRequest = new("Dave", "Password123");
        await ExecuteScopeAsync(sp => sp.GetRequiredService<IAuthService>().RegisterAsync(registerRequest, TestContext.Current.CancellationToken));

        AuthResultDto loginResult = await ExecuteScopeAsync(sp =>
            sp.GetRequiredService<IAuthService>().LoginAsync(new UserLoginDto("Dave", "Password123"), TestContext.Current.CancellationToken));

        // Act
        await ExecuteScopeAsync(sp =>
            sp.GetRequiredService<IAuthService>().RevokeSessionAsync(loginResult.RefreshToken, TestContext.Current.CancellationToken));

        // Assert
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(loginResult.RefreshToken)));
        await ExecuteDbContextAsync(async db =>
        {
            RefreshToken? token = await db.RefreshTokens.FirstOrDefaultAsync(t => t.TokenHash == hash);
            token.Should().NotBeNull();
            token.IsRevoked.Should().BeTrue();
        });
    }
}
