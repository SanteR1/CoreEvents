using Events.IntegrationTests.Infrastructure.Collections;
using Events.IntegrationTests.Infrastructure.Factories;

namespace Events.IntegrationTests.Infrastructure.Bases;

/// <summary>
/// Подтягивает коллекцию без воркера
/// </summary>
[Collection(TestCollections.ApiOnly)]
public abstract class ApiOnlyIntegrationTestBase : IntegrationTestBase<ApiOnlyIntegrationTestFactory>
{
    protected ApiOnlyIntegrationTestBase(ApiOnlyIntegrationTestFactory factory) : base(factory) { }
}
