using Users.Domain.Entities;

namespace Users.IntegrationTests.Infrastructure.Factories;

public static class TestUserFactory
{
    internal static User Create(string userName, string passwordHash, string? role = "User")
    {
        return User.Create(userName, passwordHash, role);
    }
}
