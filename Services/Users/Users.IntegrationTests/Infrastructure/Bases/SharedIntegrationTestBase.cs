using Users.IntegrationTests.Infrastructure.Collections;
using Users.IntegrationTests.Infrastructure.Factories;

namespace Users.IntegrationTests.Infrastructure.Bases;

/// <summary>
///     Подтягивает коллекцию с воркером
/// </summary>
[Collection(TestCollections.Shared)]
public abstract class SharedIntegrationTestBase : IntegrationTestBase<IntegrationTestFactory>
{
    /// <summary>
    ///     Подтягивает коллекцию с воркером
    /// </summary>
    protected SharedIntegrationTestBase(IntegrationTestFactory factory) : base(factory) { }
}
