using AwesomeAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Users.Application.Interfaces.Repositories;
using Users.Domain.Entities;
using Users.IntegrationTests.Infrastructure.Bases;
using Users.IntegrationTests.Infrastructure.Factories;

namespace Users.IntegrationTests.Repositories;

public class UserRepositoryTests(ApiOnlyIntegrationTestFactory factory) : ApiOnlyIntegrationTestBase(factory)
{
    [Fact]
    public async Task AddAndSave_ViaRepository_ShouldPersistUser()
    {
        // Arrange
        User newUser = User.Create("TestUser", "123");

        // Act
        await ExecuteScopeAsync(sp =>
        {
            IUserRepository repo = sp.GetRequiredService<IUserRepository>();
            repo.Add(newUser);

            return repo.SaveChangesAsync();
        });

        // Assert
        await ExecuteDbContextAsync(async ctx =>
        {
            bool exists = await ctx.Users.AnyAsync(e => e.Id == newUser.Id);
            exists.Should().BeTrue();
        });
    }

    [Fact]
    public async Task Add_UserWithExistingUserName_ShouldThrowDbUpdateException()
    {
        // Arrange
        User userOne = User.Create("TestUser", "123");
        await ExecuteDbContextAsync(async ctx =>
        {
            ctx.Users.Add(userOne);
            await ctx.SaveChangesAsync();
        });
        User userTwo = User.Create("TestUser", "123");
        // Act & Assert
        await ExecuteScopeAsync(async sp =>
        {
            IUserRepository repo = sp.GetRequiredService<IUserRepository>();

            Func<Task> action = async () =>
            {
                repo.Add(userTwo);
                await repo.SaveChangesAsync(TestContext.Current.CancellationToken);
            };

            await action.Should().ThrowAsync<DbUpdateException>().WithInnerException(typeof(Exception))
                        .WithMessage("*23505*");
        });
    }

    [Fact]
    public async Task GetByIdAsync_ViaRepository_ShouldReturnUser()
    {
        // Arrange
        Guid id = await ExecuteDbContextAsync(async ctx =>
        {
            User user = User.Create("TestUser", "123");
            ctx.Users.Add(user);
            await ctx.SaveChangesAsync();

            return user.Id;
        });

        // Act
        User? result = await ExecuteScopeAsync(async sp =>
        {
            IUserRepository repo = sp.GetRequiredService<IUserRepository>();

            return await repo.GetByIdAsync(id, TestContext.Current.CancellationToken);
        });

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(id);
    }

    [Fact]
    public async Task GetByUserNameAsync_ViaRepository_ShouldReturnUser()
    {
        // Arrange
        string userName = await ExecuteDbContextAsync(async ctx =>
        {
            User user = User.Create("TestUser", "123");
            ctx.Users.Add(user);
            await ctx.SaveChangesAsync();

            return user.UserName;
        });

        // Act
        User? result = await ExecuteScopeAsync(async sp =>
        {
            IUserRepository repo = sp.GetRequiredService<IUserRepository>();

            return await repo.GetByUserNameAsync(userName, TestContext.Current.CancellationToken);
        });

        // Assert
        result.Should().NotBeNull();
        result.UserName.Should().Be(userName);
    }
}
