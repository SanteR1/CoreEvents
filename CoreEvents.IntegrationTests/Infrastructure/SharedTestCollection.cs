namespace CoreEvents.IntegrationTests.Infrastructure;

public static class TestCollections
{
    public const string Shared = "Shared Test Collection";
}

[CollectionDefinition(TestCollections.Shared, DisableParallelization = true)]
public class SharedTestCollection : ICollectionFixture<IntegrationTestFactory> { }