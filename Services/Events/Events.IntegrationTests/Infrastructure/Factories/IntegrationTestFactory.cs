using Events.Infrastructure.Data;
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
using Testcontainers.Kafka;
using Testcontainers.PostgreSql;
using Testcontainers.Redis;

namespace Events.IntegrationTests.Infrastructure.Factories;

public class IntegrationTestFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _dbContainer = new PostgreSqlBuilder("postgres:18-alpine")
                                                        .WithDatabase("core_events_tests")
                                                        .WithUsername("postgres")
                                                        .WithPassword("postgres_pwd_test")
                                                        .Build();
    private readonly KafkaContainer _kafkaContainer = new KafkaBuilder("confluentinc/cp-kafka:7.9.0")
                                                      .Build();
    private readonly RedisContainer _redisContainer = new RedisBuilder("redis:8.10")
                                                      .Build();

    private Respawner? _respawner;
    private string? _connectionString;
    private string? _connectionStringKafka;
    private string? _connectionStringRedis;
    public string ConnectionString => _connectionString ?? throw new InvalidOperationException("Строка подключения не инициализирована.");
    public string ConnectionStringKafka => _connectionStringKafka ?? throw new InvalidOperationException("Строка подключения Kafka не инициализирована.");
    public string ConnectionStringRedis => _connectionStringRedis ?? throw new InvalidOperationException("Строка подключения Redis не инициализирована.");
    public async ValueTask InitializeAsync()
    {
        await Task.WhenAll(
            _dbContainer.StartAsync(),
            _kafkaContainer.StartAsync(),
            _redisContainer.StartAsync()
        );

        _connectionString = _dbContainer.GetConnectionString();
        _connectionStringKafka = _kafkaContainer.GetConnectionString();
        _connectionStringRedis = _redisContainer.GetConnectionString();

        using (var scope = Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<EventsDbContext>();
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

        builder.UseSetting("BackgroundServices:BookingInterval", "10");

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<DbContextOptions<EventsDbContext>>();

            services.AddDbContext<EventsDbContext>(options =>
                options.UseNpgsql(ConnectionString)
                    .EnableSensitiveDataLogging()
                    .EnableDetailedErrors()
                // Для тестирования можно игнорировать 
                //.ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning))
                );
        });

        Environment.SetEnvironmentVariable("Jwt__SecretKey", "test_environment_secret_key_minimum_32_characters_long_12345");
        Environment.SetEnvironmentVariable("Jwt__Issuer", "CoreEventsApi");
        Environment.SetEnvironmentVariable("Jwt__Audience", "CoreEventsClient");
        Environment.SetEnvironmentVariable("Jwt__ExpirationInMinutes", "60");

        var rawAddress = _kafkaContainer.GetBootstrapAddress();
        var cleanBootstrapAddress = rawAddress
                                    .Replace("plaintext://", "", StringComparison.OrdinalIgnoreCase)
                                    .TrimEnd('/');

        builder.ConfigureAppConfiguration((context, configBuilder) =>
        {
            var testConfig = new Dictionary<string, string?>
            {
                {"Kafka:BootstrapServers", _kafkaContainer.GetBootstrapAddress()},
                {"Redis:EndPoints",_redisContainer.GetConnectionString()},
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
            throw new InvalidOperationException("Respawner не инициализирован. Проверьте вызов InitializeAsync.");

        await using var conn = new NpgsqlConnection(ConnectionString);
        await conn.OpenAsync();

        await _respawner.ResetAsync(conn);
    }

    public override async ValueTask DisposeAsync()
    {
        await _dbContainer.StopAsync();
        await _dbContainer.DisposeAsync();
        await _kafkaContainer.StopAsync();
        await _kafkaContainer.DisposeAsync();
        await _redisContainer.StopAsync();
        await _redisContainer.DisposeAsync();
        await base.DisposeAsync();
    }
}
