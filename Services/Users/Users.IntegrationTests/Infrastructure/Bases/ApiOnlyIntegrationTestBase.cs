using Users.IntegrationTests.Infrastructure.Collections;
using Users.IntegrationTests.Infrastructure.Factories;

namespace Users.IntegrationTests.Infrastructure.Bases;

/// <summary>
///     Подтягивает коллекцию без воркера
/// </summary>
[Collection(TestCollections.ApiOnly)]
public abstract class ApiOnlyIntegrationTestBase : IntegrationTestBase<ApiOnlyIntegrationTestFactory>
{
    protected ApiOnlyIntegrationTestBase(ApiOnlyIntegrationTestFactory factory) : base(factory) { }
}
