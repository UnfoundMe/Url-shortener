using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;
using UrlShortener.Api.Contracts;
using UrlShortener.Infrastructure.Persistence;

namespace UrlShortener.IntegrationTests;

[Collection(IntegrationTestCollection.Name)]
public class RedirectEndpointTests
{
    private readonly UrlShortenerApiFactory _factory;
    private readonly HttpClient _client;

    public RedirectEndpointTests(UrlShortenerApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
    }

    [Fact]
    public async Task Get_WithExistingShortCode_Returns302WithLocationHeader()
    {
        var created = await CreateShortUrlAsync("https://example.com/redirect-target");

        var response = await _client.GetAsync($"/{created.ShortCode}");

        Assert.Equal(HttpStatusCode.Found, response.StatusCode);
        Assert.Equal("https://example.com/redirect-target", response.Headers.Location?.ToString());
    }

    [Fact]
    public async Task Get_WithUnknownShortCode_Returns404()
    {
        var response = await _client.GetAsync("/doesnotexist");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Get_PopulatesRedisCacheAfterDatabaseLookup()
    {
        var created = await CreateShortUrlAsync("https://example.com/cache-populate-check");

        // The create flow already primes the cache; delete the Redis key to force a genuine DB-driven repopulation.
        await using (var redis = await ConnectionMultiplexer.ConnectAsync(_factory.RedisConnectionString))
        {
            await redis.GetDatabase().KeyDeleteAsync($"shorturl:{created.ShortCode}");
        }

        var response = await _client.GetAsync($"/{created.ShortCode}");
        Assert.Equal(HttpStatusCode.Found, response.StatusCode);

        await using var verifyConnection = await ConnectionMultiplexer.ConnectAsync(_factory.RedisConnectionString);
        var cachedValue = await verifyConnection.GetDatabase().StringGetAsync($"shorturl:{created.ShortCode}");

        Assert.False(cachedValue.IsNullOrEmpty);
        Assert.Equal("https://example.com/cache-populate-check", cachedValue.ToString());
    }

    [Fact]
    public async Task Get_WhenRowRemovedFromDatabaseButCached_StillRedirectsFromCache()
    {
        var created = await CreateShortUrlAsync("https://example.com/cache-hit-check");

        // Prime the cache with a first request, then remove the row directly from PostgreSQL so a
        // second successful redirect can only have been served from the cache.
        await _client.GetAsync($"/{created.ShortCode}");

        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<UrlShortenerDbContext>();
            await dbContext.ShortenedUrls
                .Where(x => x.ShortCode == created.ShortCode)
                .ExecuteDeleteAsync();
        }

        var response = await _client.GetAsync($"/{created.ShortCode}");

        Assert.Equal(HttpStatusCode.Found, response.StatusCode);
        Assert.Equal("https://example.com/cache-hit-check", response.Headers.Location?.ToString());
    }

    private async Task<CreateShortUrlResponse> CreateShortUrlAsync(string url)
    {
        var response = await _client.PostAsJsonAsync("/api/urls", new CreateShortUrlRequest(url));
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<CreateShortUrlResponse>())!;
    }
}
