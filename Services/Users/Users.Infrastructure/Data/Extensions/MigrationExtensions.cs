using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Npgsql;
using Users.Infrastructure.Data;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class MigrationExtensions
    {
        private const long MigrationEventLockId = 3333333;
        public static async Task ApplyMigrationsAsync(this IHost app)
        {
            var environment = app.Services.GetRequiredService<IHostEnvironment>();

            if (environment.IsEnvironment("IntegrationTesting"))
            {
                return;
            }

            using var scope = app.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<UsersDbContext>();

            var databaseCreator = db.Database.GetService<IRelationalDatabaseCreator>();
            try
            {
                if (!await databaseCreator.ExistsAsync())
                {
                    await databaseCreator.CreateAsync();
                }
            }
            catch (PostgresException ex) when (ex.SqlState == "42P04")
            {
                // Игнорируем ошибку 42P04 (duplicate_database). 
                // Это значит, что другой под/контейнер уже успел создать базу на долю секунды раньше.
            }

            await db.Database.OpenConnectionAsync();
            try
            {
                await db.Database.ExecuteSqlRawAsync(
                    "SELECT pg_advisory_lock({0});",
                    [MigrationEventLockId]);
                await db.Database.MigrateAsync();
            }
            finally
            {
                await db.Database.ExecuteSqlRawAsync(
                    "SELECT pg_advisory_unlock({0});",
                    [MigrationEventLockId]);
                await db.Database.CloseConnectionAsync();
            }
        }

        public static async Task UseDatabaseSeedingAsync(this WebApplication app)
        {
            using var scope = app.Services.CreateScope();
            var seeder = scope.ServiceProvider.GetRequiredService<DatabaseSeeder>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<DatabaseSeeder>>();

            try
            {
                // Вызов нашего сидера
                await seeder.SeedAsync();
            }
            catch (Exception ex)
            {
                logger.LogCritical(ex, "Произошла критическая ошибка при миграции или сидировании базы данных.");
                throw;
            }
        }
    }
}
