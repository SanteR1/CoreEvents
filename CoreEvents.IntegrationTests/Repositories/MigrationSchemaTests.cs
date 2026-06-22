using CoreEvents.IntegrationTests.Infrastructure;
using Dapper;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;

namespace CoreEvents.IntegrationTests.Repositories
{
    public class MigrationSchemaTests(IntegrationTestFactory factory, ITestOutputHelper output)
        : IntegrationTestBase(factory)
    {
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

        [Fact]
        public async Task Migrations_ShouldBeUpToDate_And_NoPendingModelChanges()
        {
            await ExecuteDbContextAsync(async db =>
            {
                // Act
                var pendingMigrations = await db.Database.GetPendingMigrationsAsync();
                var hasPendingChanges = db.Database.HasPendingModelChanges();

                // Assert
                pendingMigrations.Should().BeEmpty();
                hasPendingChanges.Should().BeFalse("Модель изменилась, нужно запустить 'dotnet ef migrations add'");
            });
        }

        [Fact]
        public async Task CancellationToken_ShouldPropagateToPostgreSqlDriverAndAbortQuery()
        {
            // Arrange
            using var cts = new CancellationTokenSource();
            var ct = cts.Token;

            // Act & Assert
            // Используем pg_sleep, чтобы запрос гарантированно "завис" в базе на 5 секунд
            var task = ExecuteDbContextAsync(async ctx =>
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
                var efColumns = new List<ColumnSchemaDef>();
                var efUniques = new List<UniqueConstraintDef>();
                var efChecks = new List<CheckConstraintDef>();

                var designTimeModel = db.GetService<IDesignTimeModel>().Model;

                var entityTypes = designTimeModel.GetEntityTypes()
                    .Where(e => !e.IsOwned() && e.GetTableName() != null);

                foreach (var entity in entityTypes)
                {
                    var tableName = entity.GetTableName()!;

                    foreach (var prop in entity.GetProperties())
                    {
                        var columnName = prop.GetColumnName(StoreObjectIdentifier.Table(tableName, entity.GetSchema()));

                        var defaultObj = prop.GetDefaultValue();

                        // 1. Проверяем, является ли свойство значимым типом (не-nullable int, bool, enum, Guid и т.д.)
                        if (defaultObj != null && prop.ClrType.IsValueType && Nullable.GetUnderlyingType(prop.ClrType) == null)
                        {
                            // Динамически создаем дефолтное значение для этого типа (эквивалент default(T))
                            var clrDefaultInstance = Activator.CreateInstance(prop.ClrType);
                            // Если значение от EF Core совпадает с системным нулем C#, игнорируем его
                            if (defaultObj.Equals(clrDefaultInstance))
                            {
                                defaultObj = null;
                            }
                        }
                        // 2. Приводим к строке только реальные дефолты
                        var rawDefaultValue = prop.GetDefaultValueSql() ?? defaultObj?.ToString();
                        output.WriteLine(
                            $"✅ Найдено в БД: {tableName}: колонка: {columnName} с дефолтом: {rawDefaultValue ?? "NULL"}");

                        efColumns.Add(new ColumnSchemaDef(
                            TableName: tableName,
                            ColumnName: columnName,
                            IsNullable: prop.IsNullable,
                            DataType: prop.GetColumnType().ToLower(),
                            DefaultValue: rawDefaultValue
                        ));
                    }

                    // Уникальные ограничения (IsUnique)
                    foreach (var index in entity.GetIndexes().Where(i => i.IsUnique))
                    {
                        foreach (var prop in index.Properties)
                        {
                            var columnName = prop.GetColumnName(StoreObjectIdentifier.Table(tableName, entity.GetSchema()));

                            efUniques.Add(new UniqueConstraintDef(
                                TableName: tableName,
                                ColumnName: columnName
                            ));
                            output.WriteLine(
                                $"✅ Найдено Уникальные ограничения (IsUnique) в БД: {tableName}: колонка: {columnName}");
                        }
                    }

                    // Check ограничения (HasCheckConstraint)
                    foreach (var check in entity.GetCheckConstraints())
                    {
                        efChecks.Add(new CheckConstraintDef(
                            TableName: tableName,
                            CheckClause: check.Sql
                        ));
                        output.WriteLine(
                            $"✅ Найдено CHECK ограничение в БД: {tableName}, значение: {check.Sql}");
                    }
                }

                // ==========================================
                // 2. ACT: Получаем реальную схему через Dapper
                // ==========================================
                var dbColumnsQuery = @"
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
                var connection = db.Database.GetDbConnection();
                var dbColumns = (await connection.QueryAsync<ColumnSchemaDef>(dbColumnsQuery)).ToList();

                // Запрос уникальных индексов/ограничений
                var dbUniquesQuery = @"
            SELECT 
                t.relname AS TableName, 
                a.attname AS ColumnName
            FROM pg_class t
            JOIN pg_index ix ON t.oid = ix.indrelid
            JOIN pg_attribute a ON a.attrelid = t.oid AND a.attnum = ANY(ix.indkey)
            WHERE t.relkind = 'r' AND ix.indisunique = true;";

                var dbUniques = (await connection.QueryAsync<UniqueConstraintDef>(dbUniquesQuery)).ToList();

                // Запрос Check ограничений
                var dbChecksQuery = @"
            SELECT 
                tc.table_name AS TableName, 
                cc.check_clause AS CheckClause
            FROM information_schema.table_constraints tc
            JOIN information_schema.check_constraints cc ON tc.constraint_name = cc.constraint_name
            WHERE tc.constraint_type = 'CHECK';";

                var dbChecks = (await connection.QueryAsync<CheckConstraintDef>(dbChecksQuery)).ToList();

                // ==========================================
                // 3. ASSERT: Сверяем EF Core и Физическую БД
                // ==========================================

                foreach (var efCol in efColumns)
                {
                    var dbCol = dbColumns.SingleOrDefault(c =>
                        c.TableName == efCol.TableName &&
                        c.ColumnName == efCol.ColumnName);

                    dbCol.Should().NotBeNull($"[Ошибка схемы] Колонка '{efCol.ColumnName}' не найдена в таблице '{efCol.TableName}' в БД.");
                    dbCol.IsNullable.Should().Be(efCol.IsNullable, $"[Ошибка схемы] Таблица: '{efCol.TableName}', Колонка: '{efCol.ColumnName}'." +
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
                foreach (var efUnique in efUniques)
                {
                    var existsInDb = dbUniques.Any(u =>
                        u.TableName == efUnique.TableName &&
                        u.ColumnName == efUnique.ColumnName);
                    existsInDb.Should().BeTrue($"Unique constraint missing in DB for {efUnique.TableName}.{efUnique.ColumnName}");
                }

                // Проверка .HasCheckConstraint()
                foreach (var efCheck in efChecks)
                {
                    var existsInDb = dbChecks.Any(c =>
                        c.TableName == efCheck.TableName &&
                        NormalizeSql(c.CheckClause).Contains(NormalizeSql(efCheck.CheckClause)));
                    existsInDb.Should()
                        .BeTrue(
                            $"Check constraint: {NormalizeSql(efCheck.CheckClause)} missing in DB for table: {efCheck.TableName}");
                }
            });
            return;
            // Вспомогательный метод для нормализации SQL-строк при сравнении Check-ограничений
            static string NormalizeSql(string sql) =>
                sql.Replace("(", "")
                    .Replace(")", "")
                    .Replace(" ", "")
                    .Replace("\"", "")
                    .Replace("'", "")
                    .ToLower();
        }
    }
}
