namespace UrlShortener.IntegrationTests;

/// <summary>
/// Shares one set of PostgreSQL/Redis containers across all integration test classes so each
/// test run only pays the container-startup cost once.
/// </summary>
[CollectionDefinition(Name)]
public sealed class IntegrationTestCollection : ICollectionFixture<UrlShortenerApiFactory>
{
    public const string Name = "Integration";
}
