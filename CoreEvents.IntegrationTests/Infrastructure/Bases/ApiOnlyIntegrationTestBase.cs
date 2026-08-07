using CoreEvents.IntegrationTests.Infrastructure.Collections;
using CoreEvents.IntegrationTests.Infrastructure.Factories;

namespace CoreEvents.IntegrationTests.Infrastructure.Bases;

/// <summary>
/// Подтягивает коллекцию без воркера
/// </summary>
[Collection(TestCollections.ApiOnly)]
public abstract class ApiOnlyIntegrationTestBase : IntegrationTestBase<ApiOnlyIntegrationTestFactory>
{
    protected ApiOnlyIntegrationTestBase(ApiOnlyIntegrationTestFactory factory) : base(factory) { }
}
