using CoreEvents.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace CoreEvents.Infrastructure.Common
{
    public static class MigrationExtensions
    {
        public static async Task ApplyMigrationsAsync(this IHost app)
        {
            var environment = app.Services.GetRequiredService<IHostEnvironment>();

            if (environment.IsEnvironment("IntegrationTesting"))
            {
                return;
            }

            using var scope = app.Services.CreateScope();

            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            await db.Database.MigrateAsync();
        }
    }
}
