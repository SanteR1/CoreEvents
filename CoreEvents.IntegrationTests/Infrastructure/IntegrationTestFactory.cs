using CoreEvents.Data.DataAccess;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Npgsql;
using Respawn;
using Respawn.Graph;
using Testcontainers.PostgreSql;

namespace CoreEvents.IntegrationTests.Infrastructure;

public sealed class IntegrationTestFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _dbContainer = new PostgreSqlBuilder("postgres:18-alpine")
        .WithDatabase("core_events_tests")
        .WithUsername("postgres")
        .WithPassword("postgres_pwd_test")
        .Build();

    private Respawner? _respawner;
    private string? _connectionString;
    public string ConnectionString => _connectionString ?? throw new InvalidOperationException("Строка подключения не инициализирована.");


    public async ValueTask InitializeAsync()
    {
        await _dbContainer.StartAsync();
        _connectionString = _dbContainer.GetConnectionString();
        
        using (var scope = Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await dbContext.Database.MigrateAsync();
        }

        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync();

        _respawner = await Respawner.CreateAsync(conn, new RespawnerOptions
        {
            DbAdapter = DbAdapter.Postgres,
            SchemasToInclude = ["public"],
            TablesToIgnore = [new Table("__EFMigrationsHistory")],
            WithReseed = true
        });
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("IntegrationTesting");

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<DbContextOptions<AppDbContext>>();

            services.AddDbContext<AppDbContext>(options =>
                options.UseNpgsql(ConnectionString)
                    .EnableSensitiveDataLogging()
                    .EnableDetailedErrors()
                    // Для тестирования можно игнорировать 
                    //.ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning))
                );
        });
    }

    public async Task ResetDatabaseAsync()
    {
        if (_respawner is null)
            throw new InvalidOperationException("Respawner не инициализирован. Проверьте вызов InitializeAsync.");

        await using var conn = new NpgsqlConnection(ConnectionString);
        await conn.OpenAsync();

        await _respawner.ResetAsync(conn);
    }

    public override async ValueTask DisposeAsync()
    {
		await _dbContainer.StopAsync();
        await _dbContainer.DisposeAsync();
        await base.DisposeAsync();
    }
}