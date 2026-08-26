using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using AwesomeAssertions;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.DependencyInjection;
using Users.Application.DTOs;
using Users.Application.Interfaces.Identity;
using Users.Domain.Entities;
using Users.IntegrationTests.Infrastructure.Bases;
using Users.IntegrationTests.Infrastructure.Factories;

namespace Users.IntegrationTests.Controllers;

public class UserControllerTests(ApiOnlyIntegrationTestFactory factory) : ApiOnlyIntegrationTestBase(factory)
{
    [Fact]
    public async Task GetAnonymous_WithoutToken_ShouldReturnsOk()
    {
        // Act
        using HttpResponseMessage response =
            await HttpClient.GetAsync("/api/test-auth/anonymous", TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetProtected_WithoutToken_ShouldReturnUnauthorized()
    {
        // Act
        using HttpResponseMessage response =
            await HttpClient.GetAsync("/api/test-auth/protected", TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task LoginAndAccessAdminEndpoint_WithUserRoleToken_ShouldReturnForbidden()
    {
        // Arrange
        string userName = "User";
        string userPassword = "Password!123!";

        string passwordHash = await ExecuteScopeAsync(sp =>
        {
            IPasswordHasher passwordHasher = sp.GetRequiredService<IPasswordHasher>();

            return Task.FromResult(passwordHasher.Hash(userPassword));
        });

        User userCreate = TestUserFactory.Create(userName, passwordHash);

        await ExecuteDbContextAsync(async ctx =>
        {
            await ctx.Users.AddAsync(userCreate);
            await ctx.SaveChangesAsync();
        });

        // Act
        using HttpResponseMessage loginResponse = await HttpClient.PostAsJsonAsync(
            "/auth/login",
            new UserLoginDto(userName, userPassword),
            TestContext.Current.CancellationToken);

        // Assert
        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        string? token = await loginResponse.Content.ReadFromJsonAsync<string>(TestContext.Current.CancellationToken);
        token!.Should().NotBeNullOrWhiteSpace();

        HttpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, token);

        using HttpResponseMessage response =
            await HttpClient.GetAsync("/api/test-auth/admin-only", TestContext.Current.CancellationToken);
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task LoginAndAccessAdminEndpoint_WithValidCredentials_ReturnsOk()
    {
        // Arrange
        string userName = "Admin";
        string userPassword = "Password!123!";

        string passwordHash = await ExecuteScopeAsync(sp =>
        {
            IPasswordHasher passwordHasher = sp.GetRequiredService<IPasswordHasher>();

            return Task.FromResult(passwordHasher.Hash(userPassword));
        });

        User userCreate = TestUserFactory.Create(userName, passwordHash, "Admin");

        await ExecuteDbContextAsync(async ctx =>
        {
            await ctx.Users.AddAsync(userCreate);
            await ctx.SaveChangesAsync();
        });

        // Act
        using HttpResponseMessage loginResponse = await HttpClient.PostAsJsonAsync(
            "/auth/login",
            new UserLoginDto(userName, userPassword),
            TestContext.Current.CancellationToken);

        // Assert
        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        string? token = await loginResponse.Content.ReadFromJsonAsync<string>(TestContext.Current.CancellationToken);
        token!.Should().NotBeNullOrWhiteSpace();

        HttpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, token);

        using HttpResponseMessage response =
            await HttpClient.GetAsync("/api/test-auth/admin-only", TestContext.Current.CancellationToken);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task LoginAndAccessUserEndpoint_WithValidCredentials_ReturnsOk()
    {
        // Arrange
        string userName = "User";
        string userPassword = "Password!123!";

        string passwordHash = await ExecuteScopeAsync(sp =>
        {
            IPasswordHasher passwordHasher = sp.GetRequiredService<IPasswordHasher>();

            return Task.FromResult(passwordHasher.Hash(userPassword));
        });

        User userCreate = TestUserFactory.Create(userName, passwordHash);

        await ExecuteDbContextAsync(async ctx =>
        {
            await ctx.Users.AddAsync(userCreate);
            await ctx.SaveChangesAsync();
        });

        // Act
        using HttpResponseMessage loginResponse = await HttpClient.PostAsJsonAsync(
            "/auth/login",
            new UserLoginDto(userName, userPassword),
            TestContext.Current.CancellationToken);

        // Assert
        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        string? token = await loginResponse.Content.ReadFromJsonAsync<string>(TestContext.Current.CancellationToken);
        token!.Should().NotBeNullOrWhiteSpace();

        HttpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, token);

        using HttpResponseMessage response =
            await HttpClient.GetAsync("/api/test-auth/user-only", TestContext.Current.CancellationToken);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
