using System.Data.Common;
using AwesomeAssertions;
using Dapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Users.IntegrationTests.Infrastructure.Bases;
using Users.IntegrationTests.Infrastructure.Factories;

namespace Users.IntegrationTests.Repositories;

public class MigrationSchemaTests(ApiOnlyIntegrationTestFactory factory, ITestOutputHelper output)
    : ApiOnlyIntegrationTestBase(factory)
{
    [Fact]
    public async Task CancellationToken_ShouldPropagateToPostgreSqlDriverAndAbortQuery()
    {
        // Arrange
        using CancellationTokenSource cts = new();
        CancellationToken ct = cts.Token;

        // Act & Assert
        // Используем pg_sleep, чтобы запрос гарантированно "завис" в базе на 5 секунд
        Task task = ExecuteDbContextAsync(async ctx =>
        {
            // Выполняем сырой SQL, который заставляет БД ждать
            await ctx.Database.ExecuteSqlRawAsync("SELECT pg_sleep(5)", ct);
        });

        // Даем небольшую задержку, чтобы запрос успел дойти до сервера БД
        await Task.Delay(100, TestContext.Current.CancellationToken);

        // Отменяем токен
        await cts.CancelAsync();

        // Проверяем, что задача завершилась именно по отмене
        Func<Task> act = async () => await task;
        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    // ==========================================
    // В дополнение к тесту Schema_ShouldBeValid
    // ==========================================
    [Fact]
    public async Task DatabaseSchema_ShouldMatch_EfCoreModel()
    {
        await ExecuteDbContextAsync(async db =>
        {
            // ==========================================
            // 1. ACT: Получаем метаданные из EF Core
            // ==========================================
            List<ColumnSchemaDef> efColumns = new();
            List<UniqueConstraintDef> efUniques = new();
            List<CheckConstraintDef> efChecks = new();

            IModel designTimeModel = db.GetService<IDesignTimeModel>().Model;

            IEnumerable<IEntityType> entityTypes = designTimeModel.GetEntityTypes()
                                                                  .Where(e => !e.IsOwned() && e.GetTableName() != null);

            foreach (IEntityType entity in entityTypes)
            {
                string tableName = entity.GetTableName()!;

                foreach (IProperty prop in entity.GetProperties())
                {
                    string? columnName = prop.GetColumnName(StoreObjectIdentifier.Table(tableName, entity.GetSchema()));

                    // 1. Игнорируем системный столбец xmin во всех таблицах
                    if (columnName != null && columnName.Equals("xmin", StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    object? defaultObj = prop.GetDefaultValue();

                    // 2. Проверяем, является ли свойство значимым типом (не-nullable int, bool, enum, Guid и т.д.)
                    if (defaultObj != null && prop.ClrType.IsValueType &&
                        Nullable.GetUnderlyingType(prop.ClrType) == null)
                    {
                        // Динамически создаем дефолтное значение для этого типа (эквивалент default(T))
                        object? clrDefaultInstance = Activator.CreateInstance(prop.ClrType);
                        // Если значение от EF Core совпадает с системным нулем C#, игнорируем его
                        if (defaultObj.Equals(clrDefaultInstance))
                        {
                            defaultObj = null;
                        }
                    }

                    // 3. Приводим к строке только реальные дефолты
                    string? rawDefaultValue = prop.GetDefaultValueSql() ?? defaultObj?.ToString();
                    output.WriteLine(
                        $"✅ Найдено в БД: {tableName}: колонка: {columnName} с дефолтом: {rawDefaultValue ?? "NULL"}");

                    efColumns.Add(new ColumnSchemaDef(
                        tableName,
                        columnName,
                        prop.IsNullable,
                        prop.GetColumnType().ToLower(),
                        rawDefaultValue
                    ));
                }

                // Уникальные ограничения (IsUnique)
                foreach (IIndex index in entity.GetIndexes().Where(i => i.IsUnique))
                {
                    foreach (IProperty prop in index.Properties)
                    {
                        string? columnName =
                            prop.GetColumnName(StoreObjectIdentifier.Table(tableName, entity.GetSchema()));

                        efUniques.Add(new UniqueConstraintDef(
                            tableName,
                            columnName
                        ));
                        output.WriteLine(
                            $"✅ Найдено Уникальные ограничения (IsUnique) в БД: {tableName}: колонка: {columnName}");
                    }
                }

                // Check ограничения (HasCheckConstraint)
                foreach (ICheckConstraint check in entity.GetCheckConstraints())
                {
                    efChecks.Add(new CheckConstraintDef(
                        tableName,
                        check.Sql
                    ));
                    output.WriteLine(
                        $"✅ Найдено CHECK ограничение в БД: {tableName}, значение: {check.Sql}");
                }
            }

            // ==========================================
            // 2. ACT: Получаем реальную схему через Dapper
            // ==========================================
            string dbColumnsQuery = @"
                SELECT 
                    table_name AS TableName, 
                    column_name AS ColumnName, 
                    CASE WHEN is_nullable = 'YES' THEN true ELSE false END AS IsNullable,                     
                    CASE 
                        WHEN character_maximum_length IS NOT NULL 
                            THEN data_type || '(' || character_maximum_length || ')'
                        ELSE data_type 
                    END AS DataType, 
                    
                    column_default AS DefaultValue
                FROM information_schema.columns 
                WHERE table_schema = 'public';";
            await using DbConnection connection = db.Database.GetDbConnection();
            List<ColumnSchemaDef> dbColumns = (await connection.QueryAsync<ColumnSchemaDef>(dbColumnsQuery)).ToList();

            // Запрос уникальных индексов/ограничений
            string dbUniquesQuery = @"
            SELECT 
                t.relname AS TableName, 
                a.attname AS ColumnName
            FROM pg_class t
            JOIN pg_index ix ON t.oid = ix.indrelid
            JOIN pg_attribute a ON a.attrelid = t.oid AND a.attnum = ANY(ix.indkey)
            WHERE t.relkind = 'r' AND ix.indisunique = true;";

            List<UniqueConstraintDef> dbUniques =
                (await connection.QueryAsync<UniqueConstraintDef>(dbUniquesQuery)).ToList();

            // Запрос Check ограничений
            string dbChecksQuery = @"
            SELECT 
                tc.table_name AS TableName, 
                cc.check_clause AS CheckClause
            FROM information_schema.table_constraints tc
            JOIN information_schema.check_constraints cc ON tc.constraint_name = cc.constraint_name
            WHERE tc.constraint_type = 'CHECK';";

            List<CheckConstraintDef> dbChecks =
                (await connection.QueryAsync<CheckConstraintDef>(dbChecksQuery)).ToList();

            // ==========================================
            // 3. ASSERT: Сверяем EF Core и Физическую БД
            // ==========================================

            foreach (ColumnSchemaDef efCol in efColumns)
            {
                ColumnSchemaDef? dbCol = dbColumns.SingleOrDefault(c =>
                    c.TableName == efCol.TableName &&
                    c.ColumnName == efCol.ColumnName);

                dbCol.Should()
                     .NotBeNull(
                         $"[Ошибка схемы] Колонка '{efCol.ColumnName}' не найдена в таблице '{efCol.TableName}' в БД.");
                dbCol.IsNullable.Should().Be(efCol.IsNullable,
                    $"[Ошибка схемы] Таблица: '{efCol.TableName}', Колонка: '{efCol.ColumnName}'." +
                    $" EF Core ожидает IsNullable({efCol.IsNullable}), но в физической БД он IsNullable({dbCol.IsNullable})");
                dbCol.DataType.Should().ContainEquivalentOf(efCol.DataType);

                if (efCol.DefaultValue != null)
                {
                    dbCol.DefaultValue.Should()
                         .NotBeNull($"[Ошибка схемы] Таблица: '{efCol.TableName}', Колонка: '{efCol.ColumnName}'." +
                                    $" EF Core ожидает default '{efCol.DefaultValue}', но в физической БД он отсутствует (NULL)")
                         .And.ContainEquivalentOf(efCol.DefaultValue);
                }
            }

            // Проверка .IsUnique()
            foreach (UniqueConstraintDef efUnique in efUniques)
            {
                bool existsInDb = dbUniques.Any(u =>
                    u.TableName == efUnique.TableName &&
                    u.ColumnName == efUnique.ColumnName);
                existsInDb.Should()
                          .BeTrue($"Unique constraint missing in DB for {efUnique.TableName}.{efUnique.ColumnName}");
            }

            // Проверка .HasCheckConstraint()
            foreach (CheckConstraintDef efCheck in efChecks)
            {
                bool existsInDb = dbChecks.Any(c =>
                    c.TableName == efCheck.TableName &&
                    NormalizeSql(c.CheckClause).Contains(NormalizeSql(efCheck.CheckClause)));
                existsInDb.Should()
                          .BeTrue(
                              $"Check constraint: {NormalizeSql(efCheck.CheckClause)} missing in DB for table: {efCheck.TableName}");
            }
        });

        return;

        // Вспомогательный метод для нормализации SQL-строк при сравнении Check-ограничений
        static string NormalizeSql(string sql)
        {
            return sql.Replace("(", "")
                      .Replace(")", "")
                      .Replace(" ", "")
                      .Replace("\"", "")
                      .Replace("'", "")
                      .ToLower();
        }
    }

    [Fact]
    public async Task Migrations_ShouldBeUpToDate_And_NoPendingModelChanges()
    {
        await ExecuteDbContextAsync(async db =>
        {
            // Act
            IEnumerable<string> pendingMigrations = await db.Database.GetPendingMigrationsAsync();
            bool hasPendingChanges = db.Database.HasPendingModelChanges();

            // Assert
            pendingMigrations.Should().BeEmpty();
            hasPendingChanges.Should().BeFalse("Модель изменилась, нужно запустить 'dotnet ef migrations add'");
        });
    }

    [Fact]
    public async Task Schema_ShouldBeValid()
    {
        // Act & Assert
        await ExecuteDbContextAsync(db =>
        {
            AssertSchemaMatches(db);

            return Task.CompletedTask;
        });
    }

    public record ColumnSchemaDef(
        string TableName,
        string? ColumnName,
        bool IsNullable,
        string DataType,
        string? DefaultValue);

    public record UniqueConstraintDef(
        string TableName,
        string? ColumnName);

    public record CheckConstraintDef(
        string TableName,
        string CheckClause);
}
