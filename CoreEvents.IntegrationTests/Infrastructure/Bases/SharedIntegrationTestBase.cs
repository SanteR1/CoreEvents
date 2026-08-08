using CoreEvents.IntegrationTests.Infrastructure.Collections;
using CoreEvents.IntegrationTests.Infrastructure.Factories;

namespace CoreEvents.IntegrationTests.Infrastructure.Bases;

/// <summary>
/// Подтягивает коллекцию с воркером
/// </summary>
[Collection(TestCollections.Shared)]
public abstract class SharedIntegrationTestBase : IntegrationTestBase<IntegrationTestFactory>
{
    /// <summary>
    /// Подтягивает коллекцию с воркером
    /// </summary>
    protected SharedIntegrationTestBase(IntegrationTestFactory factory) : base(factory) { }
}
