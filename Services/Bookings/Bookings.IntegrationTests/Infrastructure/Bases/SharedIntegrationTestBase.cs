using Bookings.IntegrationTests.Infrastructure.Collections;
using Bookings.IntegrationTests.Infrastructure.Factories;

namespace Bookings.IntegrationTests.Infrastructure.Bases;

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
