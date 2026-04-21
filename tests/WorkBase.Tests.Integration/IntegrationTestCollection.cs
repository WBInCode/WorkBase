using Xunit;

namespace WorkBase.Tests.Integration;

[CollectionDefinition("Integration")]
public class IntegrationTestCollection : ICollectionFixture<WorkBaseWebFactory>
{
    // This class has no code; it is only used to define the test collection
    // and ensure a single WorkBaseWebFactory is shared across all test classes.
}
