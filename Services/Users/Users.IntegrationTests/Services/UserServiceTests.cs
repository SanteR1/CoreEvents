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

            return authService.RegisterAsync(registerRequest);
        });

        UserLoginDto loginDto = new("UserName", "123Password");

        // Act & Assert
        await Assert.ThrowsAsync<InvalidCredentialsException>(() =>
            ExecuteScopeAsync(sp =>
            {
                IAuthService authService = sp.GetRequiredService<IAuthService>();

                return authService.LoginAsync(loginDto);
            }));
    }

    [Fact]
    public async Task LoginAsync_WithValidCredentials_ReturnsToken()
    {
        // Arrange
        UserRegisterDto registerRequest = new("UserName", "Password123");

        await ExecuteScopeAsync(sp =>
        {
            IAuthService authService = sp.GetRequiredService<IAuthService>();

            return authService.RegisterAsync(registerRequest);
        });

        UserLoginDto loginDto = new(registerRequest.UserName, registerRequest.Password);

        // Act
        string token = await ExecuteScopeAsync(sp =>
        {
            IAuthService authService = sp.GetRequiredService<IAuthService>();

            return authService.LoginAsync(loginDto);
        });

        // Assert
        Assert.False(string.IsNullOrWhiteSpace(token));
    }

    [Fact]
    public async Task RegisterAsync_WithExistingUserName_ThrowsUserAlreadyExistsException()
    {
        // Arrange
        UserRegisterDto request = new("UserName", "Password123");

        await ExecuteScopeAsync(sp =>
        {
            IAuthService authService = sp.GetRequiredService<IAuthService>();

            return authService.RegisterAsync(request);
        });

        // Act & Assert
        await Assert.ThrowsAsync<UserAlreadyExistsException>(() =>
            ExecuteScopeAsync(sp =>
            {
                IAuthService authService = sp.GetRequiredService<IAuthService>();

                return authService.RegisterAsync(request);
            }));
    }

    [Fact]
    public async Task RegisterAsync_WithValidData_CreatesUserAndReturnsRoleIsUserAndToken()
    {
        // Arrange
        UserRegisterDto request = new("UserName", "Password123");

        // Act
        string token = await ExecuteScopeAsync(sp =>
        {
            IAuthService authService = sp.GetRequiredService<IAuthService>();

            return authService.RegisterAsync(request);
        });

        // Assert
        Assert.False(string.IsNullOrWhiteSpace(token));

        await ExecuteDbContextAsync(async db =>
        {
            User? savedUser = await db.Users.FirstOrDefaultAsync(u => u.UserName == request.UserName);
            savedUser.Should().NotBeNull();
            savedUser.UserName.Should().Be(request.UserName);
            savedUser.Role.Should().Be(RoleName.User);
        });
    }
}
