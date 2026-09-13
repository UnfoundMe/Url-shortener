using System.Net;
using System.Net.Http.Json;
using UrlShortener.Api.Contracts;

namespace UrlShortener.IntegrationTests;

[Collection(IntegrationTestCollection.Name)]
public class CreateShortUrlEndpointTests
{
    private readonly HttpClient _client;

    public CreateShortUrlEndpointTests(UrlShortenerApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Post_WithValidUrl_Returns201WithShortCode()
    {
        var response = await _client.PostAsJsonAsync("/api/urls", new CreateShortUrlRequest("https://example.com/page"));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<CreateShortUrlResponse>();
        Assert.NotNull(body);
        Assert.Equal(7, body!.ShortCode.Length);
        Assert.Equal("https://example.com/page", body.OriginalUrl);
        Assert.Contains(body.ShortCode, body.ShortUrl);
    }

    [Theory]
    [InlineData("not-a-url")]
    [InlineData("ftp://example.com")]
    [InlineData("")]
    public async Task Post_WithInvalidUrl_Returns400(string url)
    {
        var response = await _client.PostAsJsonAsync("/api/urls", new CreateShortUrlRequest(url));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Post_WithUrlExceedingMaxLength_Returns400()
    {
        var longUrl = "https://example.com/" + new string('a', 3000);

        var response = await _client.PostAsJsonAsync("/api/urls", new CreateShortUrlRequest(longUrl));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Post_CalledTwiceForSameUrl_GeneratesDifferentShortCodes()
    {
        var request = new CreateShortUrlRequest("https://example.com/duplicate-check");

        var first = await (await _client.PostAsJsonAsync("/api/urls", request)).Content.ReadFromJsonAsync<CreateShortUrlResponse>();
        var second = await (await _client.PostAsJsonAsync("/api/urls", request)).Content.ReadFromJsonAsync<CreateShortUrlResponse>();

        Assert.NotEqual(first!.ShortCode, second!.ShortCode);
    }
}
