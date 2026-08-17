using Events.IntegrationTests.Infrastructure.Collections;
using Events.IntegrationTests.Infrastructure.Factories;

namespace Events.IntegrationTests.Infrastructure.Bases;

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
