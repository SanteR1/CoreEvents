using Bookings.IntegrationTests.Infrastructure.Collections;
using Bookings.IntegrationTests.Infrastructure.Factories;

namespace Bookings.IntegrationTests.Infrastructure.Bases;

/// <summary>
/// Подтягивает коллекцию без воркера
/// </summary>
[Collection(TestCollections.ApiOnly)]
public abstract class ApiOnlyIntegrationTestBase : IntegrationTestBase<ApiOnlyIntegrationTestFactory>
{
    protected ApiOnlyIntegrationTestBase(ApiOnlyIntegrationTestFactory factory) : base(factory) { }
}
