using Bookings.IntegrationTests.Infrastructure.Factories;

namespace Bookings.IntegrationTests.Infrastructure.Collections;

public static class TestCollections
{
    public const string Shared = "Shared Test Collection";
    public const string ApiOnly = "ApiOnly Test Collection";
    public const string FaultInjection = "RollbackFaultInjection Test Collection";
}

[CollectionDefinition(TestCollections.Shared, DisableParallelization = true)]
public class SharedTestCollection : ICollectionFixture<IntegrationTestFactory> { }

[CollectionDefinition(TestCollections.ApiOnly, DisableParallelization = true)]
public class ApiOnlyTestCollection : ICollectionFixture<ApiOnlyIntegrationTestFactory> { }

[CollectionDefinition(TestCollections.FaultInjection, DisableParallelization = true)]
public class FaultInjectionTestCollection : ICollectionFixture<FaultInjectionTestFactory> { }
