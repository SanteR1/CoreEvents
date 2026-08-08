using CoreEvents.Application.Interfaces.Repositories;
using CoreEvents.Domain.Entities;
using CoreEvents.IntegrationTests.Infrastructure.Bases;
using CoreEvents.IntegrationTests.Infrastructure.Factories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CoreEvents.IntegrationTests.Repositories;

public class UserRepositoryTests(ApiOnlyIntegrationTestFactory factory) : ApiOnlyIntegrationTestBase(factory)
{
    [Fact]
    public async Task AddAndSave_ViaRepository_ShouldPersistUser()
    {
        // Arrange
        var newUser = User.Create("TestUser", "123");

        // Act
        await ExecuteScopeAsync(sp =>
        {
            var repo = sp.GetRequiredService<IUserRepository>();
            repo.Add(newUser);
            return repo.SaveChangesAsync();
        });

        // Assert
        await ExecuteDbContextAsync(async ctx =>
        {
            var exists = await ctx.Users.AnyAsync(e => e.Id == newUser.Id);
            exists.Should().BeTrue();
        });
    }
    [Fact]
    public async Task GetByIdAsync_ViaRepository_ShouldReturnUser()
    {
        // Arrange
        var id = await ExecuteDbContextAsync(async ctx =>
        {
            var user = User.Create("TestUser", "123");
            ctx.Users.Add(user);
            await ctx.SaveChangesAsync();
            return user.Id;
        });

        // Act
        var result = await ExecuteScopeAsync(async sp =>
        {
            var repo = sp.GetRequiredService<IUserRepository>();
            return await repo.GetByIdAsync(id, TestContext.Current.CancellationToken);

        });

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(id);
    }

    [Fact]
    public async Task Add_UserWithExistingUserName_ShouldThrowDbUpdateException()
    {
        // Arrange
        var userOne = User.Create("TestUser", "123");
        await ExecuteDbContextAsync(async ctx =>
        {
            ctx.Users.Add(userOne);
            await ctx.SaveChangesAsync();
        });
        var userTwo = User.Create("TestUser", "123");
        // Act & Assert
        await ExecuteScopeAsync(async sp =>
        {
            var repo = sp.GetRequiredService<IUserRepository>();

            Func<Task> action = async () =>
            {
                repo.Add(userTwo);
                await repo.SaveChangesAsync(TestContext.Current.CancellationToken);
            };

            await action.Should().ThrowAsync<DbUpdateException>().WithInnerException(typeof(Exception)).WithMessage("*23505*");
        });
    }

    [Fact]
    public async Task GetByUserNameAsync_ViaRepository_ShouldReturnUser()
    {
        // Arrange
        var userName = await ExecuteDbContextAsync(async ctx =>
        {
            var user = User.Create("TestUser", "123");
            ctx.Users.Add(user);
            await ctx.SaveChangesAsync();
            return user.UserName;
        });

        // Act
        var result = await ExecuteScopeAsync(async sp =>
        {
            var repo = sp.GetRequiredService<IUserRepository>();
            return await repo.GetByUserNameAsync(userName, TestContext.Current.CancellationToken);

        });

        // Assert
        result.Should().NotBeNull();
        result.UserName.Should().Be(userName);
    }
}
