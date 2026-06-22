using CoreEvents.Data.DataAccess;
using EfSchemaCompare;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CoreEvents.IntegrationTests.Infrastructure;

/// <summary>
/// Базовый класс для интеграционных тестов системы.
/// Обеспечивает запуск на единой инфраструктуре (Testcontainers/WebApplicationFactory),
/// изоляцию DI-контейнеров для каждого этапа теста и автоматическую очистку БД.
/// </summary>
[Collection(TestCollections.Shared)]
public abstract class IntegrationTestBase : IAsyncLifetime
{
    protected readonly IntegrationTestFactory Factory;

    /// <summary>
    /// Инициализирует базовый класс тестов, получая глобальный экземпляр фабрики.
    /// </summary>
    /// <param name="factory">Глобальная фабрика, созданная один раз для всей коллекции тестов.</param>
    protected IntegrationTestBase(IntegrationTestFactory factory)
    {
        Factory = factory;
    }

    /// <summary>
    /// Выполняется асинхронно фреймворком xUnit ПЕРЕД каждым тестом ([Fact]).
    /// Сбрасывает состояние базы данных до первоначального (чистого), чтобы тесты не влияли друг на друга.
    /// </summary>
    public async ValueTask InitializeAsync() => await Factory.ResetDatabaseAsync();

    /// <summary>
    /// Выполняется асинхронно фреймворком xUnit ПОСЛЕ каждого теста ([Fact]).
    /// Обычно остается пустым, так как очистка данных происходит перед тестом.
    /// </summary>
    public virtual ValueTask DisposeAsync() => ValueTask.CompletedTask;

    /// <summary>
    /// Создает новую изолированную область видимости (Scope) DI-контейнера, 
    /// выполняет действие и возвращает результат.
    /// </summary>
    /// <remarks>
    /// **Когда использовать:** На этапе <c>Act</c> (выполнение), когда нужно вызвать тестируемый сервис 
    /// и получить от него результат (например, ID созданной сущности).
    /// </remarks>
    /// <typeparam name="T">Тип возвращаемого значения.</typeparam>
    /// <param name="action">Асинхронный делегат, принимающий IServiceProvider для извлечения зависимостей.</param>
    /// <returns>Результат выполнения переданного действия.</returns>
    protected async Task<T> ExecuteScopeAsync<T>(Func<IServiceProvider, Task<T>> action)
    {
        using var scope = Factory.Services.CreateScope();
        return await action(scope.ServiceProvider);
    }

    /// <summary>
    /// Создает новую изолированную область видимости (Scope) DI-контейнера 
    /// и выполняет действие без возврата результата.
    /// </summary>
    /// <remarks>
    /// **Когда использовать:** На этапе <c>Act</c> (выполнение), когда тестируемый метод сервиса возвращает <c>Task</c> (ничего не возвращает).
    /// </remarks>
    /// <param name="action">Асинхронный делегат, принимающий IServiceProvider для извлечения зависимостей.</param>
    protected async Task ExecuteScopeAsync(Func<IServiceProvider, Task> action)
    {
        using var scope = Factory.Services.CreateScope();
        await action(scope.ServiceProvider);
    }

    /// <summary>
    /// Создает изолированный Scope, автоматически извлекает <see cref="AppDbContext"/> 
    /// и выполняет операцию с базой данных без возврата результата.
    /// </summary>
    /// <remarks>
    /// **Когда использовать:** <list type="bullet">
    /// <item>В фазе <c>Arrange</c> для подготовки начальных данных (запись в БД).</item>
    /// <item>В фазе <c>Assert</c> для проверки итогового состояния базы данных (чтение из БД).</item>
    /// </list>
    /// Гарантирует, что используется чистый контекст без закэшированных данных в Change Tracker.
    /// </remarks>
    /// <param name="action">Асинхронный делегат, принимающий экземпляр контекста БД.</param>
    internal Task ExecuteDbContextAsync(Func<AppDbContext, Task> action)
        => ExecuteScopeAsync(sp => action(sp.GetRequiredService<AppDbContext>()));

    /// <summary>
    /// Создает изолированный Scope, автоматически извлекает <see cref="AppDbContext"/>, 
    /// выполняет операцию с БД и возвращает результат.
    /// </summary>
    /// <remarks>
    /// **Когда использовать:** В фазе <c>Arrange</c>, когда необходимо создать тестовые данные 
    /// в базе и вернуть их идентификаторы для дальнейшего использования в тесте.
    /// </remarks>
    /// <typeparam name="T">Тип возвращаемого значения (например, Guid или int).</typeparam>
    /// <param name="action">Асинхронный делегат, принимающий контекст БД и возвращающий результат.</param>
    /// <returns>Данные, извлеченные или сгенерированные при работе с БД.</returns>
    internal Task<T> ExecuteDbContextAsync<T>(Func<AppDbContext, Task<T>> action)
        => ExecuteScopeAsync(sp => action(sp.GetRequiredService<AppDbContext>()));

    /// <summary>
    /// Универсальный метод работы с любым типом DbContext (если в проекте используется несколько контекстов).
    /// Выполняет действие без возврата результата.
    /// </summary>
    /// <remarks>
    /// **Когда использовать:** Аналогично <see cref="ExecuteDbContextAsync(Func{AppDbContext, Task})"/> (Arrange/Assert), 
    /// но для дополнительных контекстов БД (например, IdentityDbContext или контексты других микросервисов).
    /// </remarks>
    /// <typeparam name="TContext">Тип контекста базы данных.</typeparam>
    /// <param name="action">Асинхронный делегат, принимающий специфичный DbContext.</param>
    internal Task ExecuteDbContextAsync<TContext>(Func<TContext, Task> action)
        where TContext : DbContext
        => ExecuteScopeAsync(sp => action(sp.GetRequiredService<TContext>()));

    /// <summary>
    /// Универсальный метод работы с любым типом DbContext (если в проекте используется несколько контекстов).
    /// Выполняет действие и возвращает результат.
    /// </summary>
    /// <remarks>
    /// **Когда использовать:** Аналогично <see cref="ExecuteDbContextAsync{T}(Func{AppDbContext, Task{T}})"/> (Arrange), 
    /// но для альтернативных контекстов БД.
    /// </remarks>
    /// <typeparam name="TContext">Тип контекста базы данных.</typeparam>
    /// <typeparam name="TResult">Тип возвращаемого значения.</typeparam>
    /// <param name="action">Асинхронный делегат, принимающий специфичный DbContext.</param>
    /// <returns>Результат операции с базой данных.</returns>
    internal Task<TResult> ExecuteDbContextAsync<TContext, TResult>(Func<TContext, Task<TResult>> action)
        where TContext : DbContext
        => ExecuteScopeAsync(sp => action(sp.GetRequiredService<TContext>()));

    /// <summary>
    /// Проверяет соответствие физической схемы базы данных текущей модели EF Core.
    /// </summary>
    /// <param name="db">Экземпляр <see cref="AppDbContext"/>, используемый в тесте.</param>
    /// <exception cref="Xunit.Sdk.EqualException">Выбрасывается, если схема БД отличается от модели.</exception>
    internal void AssertSchemaMatches(AppDbContext db)
    {
        var comparer = new CompareEfSql();

        // Выполняем сравнение, используя полную строку подключения из фабрики
        var hasErrors = comparer.CompareEfWithDb(Factory.ConnectionString, db);

        // False, так как hasErrors == true означает наличие проблем
        hasErrors.Should().BeFalse(comparer.GetAllErrors);
    }
}