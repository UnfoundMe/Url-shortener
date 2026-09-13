using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using UrlShortener.Api.Contracts;

namespace UrlShortener.IntegrationTests;

[Collection(IntegrationTestCollection.Name)]
public class EndToEndFlowTests
{
    private readonly UrlShortenerApiFactory _factory;

    public EndToEndFlowTests(UrlShortenerApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task FullFlow_CreateThenRedirect_ReturnsShortUrlThen302ToOriginalUrl()
    {
        using var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var createResponse = await client.PostAsJsonAsync("/api/urls", new CreateShortUrlRequest("https://example.com/end-to-end"));
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        var created = await createResponse.Content.ReadFromJsonAsync<CreateShortUrlResponse>();
        Assert.NotNull(created);

        var redirectResponse = await client.GetAsync($"/{created!.ShortCode}");

        Assert.Equal(HttpStatusCode.Found, redirectResponse.StatusCode);
        Assert.Equal("https://example.com/end-to-end", redirectResponse.Headers.Location?.ToString());
    }
}
