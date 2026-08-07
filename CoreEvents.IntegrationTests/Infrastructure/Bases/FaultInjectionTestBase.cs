using CoreEvents.IntegrationTests.Infrastructure.Collections;
using CoreEvents.IntegrationTests.Infrastructure.Factories;
using CoreEvents.IntegrationTests.Infrastructure.FaultInjection;
using Microsoft.Extensions.DependencyInjection;

namespace CoreEvents.IntegrationTests.Infrastructure.Bases;

/// <summary>
/// Подтягивает коллекцию без воркера
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
