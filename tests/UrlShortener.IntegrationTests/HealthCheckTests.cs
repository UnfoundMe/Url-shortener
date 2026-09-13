using System.Net;

namespace UrlShortener.IntegrationTests;

[Collection(IntegrationTestCollection.Name)]
public class HealthCheckTests
{
    private readonly HttpClient _client;

    public HealthCheckTests(UrlShortenerApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task HealthLive_ReturnsHealthy()
    {
        var response = await _client.GetAsync("/health/live");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task HealthReady_WithHealthyDependencies_ReturnsHealthy()
    {
        var response = await _client.GetAsync("/health/ready");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
