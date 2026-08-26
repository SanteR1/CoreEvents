using Microsoft.Extensions.DependencyInjection;
using Users.IntegrationTests.Infrastructure.Collections;
using Users.IntegrationTests.Infrastructure.Factories;
using Users.IntegrationTests.Infrastructure.FaultInjection;

namespace Users.IntegrationTests.Infrastructure.Bases;

/// <summary>
///     Подтягивает коллекцию без воркера
/// </summary>
[Collection(TestCollections.FaultInjection)]
public abstract class FaultInjectionTestBase : IntegrationTestBase<FaultInjectionTestFactory>
{
    // Состояние сбоев доступно всем тестам, которые наследуются от этого класса
    protected readonly FaultInjectionState State;

    protected FaultInjectionTestBase(FaultInjectionTestFactory factory) : base(factory)
    {
        // Извлекаем Singleton-состояние из фабрики
        State = factory.Services.GetRequiredService<FaultInjectionState>();

        // Гарантированно очищаем настройки сбоев перед каждым новым [Fact]
        State.Reset();
    }
}
