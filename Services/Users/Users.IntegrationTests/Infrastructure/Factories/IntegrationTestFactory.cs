using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Npgsql;
using Respawn;
using Respawn.Graph;
using Testcontainers.PostgreSql;
using Users.Infrastructure.Data;

namespace Users.IntegrationTests.Infrastructure.Factories;

public class IntegrationTestFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _dbContainer = new PostgreSqlBuilder("postgres:18-alpine")
                                                        .WithDatabase("core_events_tests")
                                                        .WithUsername("postgres")
                                                        .WithPassword("postgres_pwd_test")
                                                        .Build();

    private string? _connectionString;
    private Respawner? _respawner;

    public string ConnectionString => _connectionString ??
                                      throw new InvalidOperationException("Строка подключения не инициализирована.");

    public async ValueTask InitializeAsync()
    {
        await Task.WhenAll(
            _dbContainer.StartAsync()
        );

        _connectionString = _dbContainer.GetConnectionString();

        using (IServiceScope scope = Services.CreateScope())
        {
            UsersDbContext dbContext = scope.ServiceProvider.GetRequiredService<UsersDbContext>();
            await dbContext.Database.MigrateAsync();
        }

        await using NpgsqlConnection conn = new(_connectionString);
        await conn.OpenAsync();

        _respawner = await Respawner.CreateAsync(conn,
            new RespawnerOptions
            {
                DbAdapter = DbAdapter.Postgres,
                SchemasToInclude = ["public"],
                TablesToIgnore = [new Table("__EFMigrationsHistory")],
                WithReseed = true
            });
    }

    /// <summary>
    ///     Асинхронно освобождает ресурсы фабрики: сначала останавливает тестовый хост
    ///     приложения (WebApplicationFactory), затем параллельно останавливает и удаляет
    ///     все Docker-контейнеры (Postgres, Kafka, Redis), поднятые для теста.
    /// </summary>
    /// <returns></returns>
    public override async ValueTask DisposeAsync()
    {
        await base.DisposeAsync();
        await _dbContainer.DisposeAsync();
        GC.SuppressFinalize(this);
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("IntegrationTesting");

        builder.UseSetting("BackgroundServices:BookingInterval", "10");

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<DbContextOptions<UsersDbContext>>();

            services.AddDbContext<UsersDbContext>(options =>
                    options.UseNpgsql(ConnectionString)
                           .EnableSensitiveDataLogging()
                           .EnableDetailedErrors()
            // Для тестирования можно игнорировать 
            //.ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning))
            );
        });

        Environment.SetEnvironmentVariable("Jwt__SecretKey",
            "test_environment_secret_key_minimum_32_characters_long_12345");
        Environment.SetEnvironmentVariable("Jwt__Issuer", "CoreEventsApi");
        Environment.SetEnvironmentVariable("Jwt__Audience", "CoreEventsClient");
        Environment.SetEnvironmentVariable("Jwt__ExpirationInMinutes", "60");

        builder.ConfigureAppConfiguration((context, configBuilder) =>
        {
            Dictionary<string, string?> testConfig = new()
            {
                // Задаем фиксированный фейковый JWT-секрет только для тестов
                { "Jwt:SecretKey", "test_environment_secret_key_minimum_32_characters_long_12345" },
                { "Jwt:Issuer", "CoreEventsApi" },
                { "Jwt:Audience", "CoreEventsClient" },
                { "Jwt:ExpirationInMinutes", "60" }
            };

            configBuilder.AddInMemoryCollection(testConfig);
        });
    }

    public async Task ResetDatabaseAsync()
    {
        if (_respawner is null)
        {
            throw new InvalidOperationException("Respawner не инициализирован. Проверьте вызов InitializeAsync.");
        }

        await using NpgsqlConnection conn = new(ConnectionString);
        await conn.OpenAsync();

        await _respawner.ResetAsync(conn);
    }
}
