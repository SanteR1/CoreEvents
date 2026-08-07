using Bookings.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Hosting;
using Npgsql;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class MigrationExtensions
    {
        private const long MigrationEventLockId = 2222222;
        public static async Task ApplyMigrationsAsync(this IHost app)
        {
            var environment = app.Services.GetRequiredService<IHostEnvironment>();

            if (environment.IsEnvironment("IntegrationTesting"))
            {
                return;
            }

            using var scope = app.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<BookingsDbContext>();

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
    }
}
