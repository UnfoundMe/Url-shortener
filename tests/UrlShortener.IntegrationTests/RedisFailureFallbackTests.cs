using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Testcontainers.PostgreSql;
using UrlShortener.Api.Contracts;

namespace UrlShortener.IntegrationTests;

/// <summary>
/// Boots the API against a real PostgreSQL container but a deliberately unreachable Redis
/// endpoint, proving that redirects still work end-to-end when the cache is down.
/// Kept out of <see cref="IntegrationTestCollection"/> since it needs its own, differently
/// configured factory rather than the shared healthy one.
/// </summary>
public sealed class RedisFailureFallbackTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:16-alpine")
        .WithDatabase("urlshortener")
        .WithUsername("urlshortener")
        .WithPassword("urlshortener_test")
        .Build();

    private WebApplicationFactory<Program> _factory = null!;

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();

        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((_, configBuilder) =>
            {
                configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:Postgres"] = _postgres.GetConnectionString(),
                    // Port 1 is reserved and never accepts connections, simulating Redis being down.
                    ["ConnectionStrings:Redis"] = "127.0.0.1:1,abortConnect=false,connectTimeout=500",
                    ["Cache:ConnectTimeoutMs"] = "500",
                    ["Cache:OperationTimeoutMs"] = "500",
                });
            });
        });
    }

    public async Task DisposeAsync()
    {
        _factory.Dispose();
        await _postgres.DisposeAsync();
    }

    [Fact]
    public async Task Get_WhenRedisIsUnreachable_StillRedirectsFromPostgres()
    {
        using var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var createResponse = await client.PostAsJsonAsync("/api/urls", new CreateShortUrlRequest("https://example.com/redis-down"));
        createResponse.EnsureSuccessStatusCode();
        var created = await createResponse.Content.ReadFromJsonAsync<CreateShortUrlResponse>();

        var redirectResponse = await client.GetAsync($"/{created!.ShortCode}");

        Assert.Equal(HttpStatusCode.Found, redirectResponse.StatusCode);
        Assert.Equal("https://example.com/redis-down", redirectResponse.Headers.Location?.ToString());
    }

    [Fact]
    public async Task HealthReady_WhenRedisIsUnreachable_StillReturnsHealthyBecauseRedisIsASoftDependency()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync("/health/ready");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
