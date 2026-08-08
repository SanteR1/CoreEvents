using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Users.Application.Interfaces.Identity;
using Users.Domain.Entities;

namespace Users.Infrastructure.Data;

sealed class DatabaseSeeder(UsersDbContext dbContext, IConfiguration configuration, IPasswordHasher passwordHasher, ILogger<DatabaseSeeder> logger)
{
    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        var adminLogin = configuration["AdminSettings:Login"];
        var adminPassword = configuration["AdminSettings:Password"];

        if (string.IsNullOrWhiteSpace(adminLogin) || string.IsNullOrWhiteSpace(adminPassword))
        {
            logger.LogWarning("Учетные данные администратора не найдены в конфигурации. Пропуск сидирования.");
            return;
        }

        var adminExists = await dbContext.Users
            .AnyAsync(u => u.UserName == adminLogin, cancellationToken);

        if (!adminExists)
        {
            var passwordHash = passwordHasher.Hash(adminPassword);
            var adminUser = User.Create(adminLogin, passwordHash, "Admin");
            dbContext.Users.Add(adminUser);
            await dbContext.SaveChangesAsync(cancellationToken);
            logger.LogInformation("Пользователь-администратор {Email} успешно создан.", adminLogin);
        }
        else
        {
            logger.LogInformation("Администратор {Email} уже существует в базе данных.", adminLogin);
        }
    }
}
