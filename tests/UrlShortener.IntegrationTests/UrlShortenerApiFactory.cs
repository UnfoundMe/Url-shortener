using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Testcontainers.PostgreSql;
using Testcontainers.Redis;

namespace UrlShortener.IntegrationTests;

/// <summary>
/// Spins up real PostgreSQL and Redis containers via Testcontainers and wires the API under
/// test to them, so integration tests exercise the actual EF Core / StackExchange.Redis stack
/// rather than in-memory fakes.
/// </summary>
public sealed class UrlShortenerApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:16-alpine")
        .WithDatabase("urlshortener")
        .WithUsername("urlshortener")
        .WithPassword("urlshortener_test")
        .Build();

    private readonly RedisContainer _redis = new RedisBuilder("redis:7-alpine")
        .Build();

    public string RedisConnectionString => _redis.GetConnectionString();

    public async Task InitializeAsync()
    {
        await Task.WhenAll(_postgres.StartAsync(), _redis.StartAsync());
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, configBuilder) =>
        {
            configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Postgres"] = _postgres.GetConnectionString(),
                ["ConnectionStrings:Redis"] = _redis.GetConnectionString(),
                ["Cache:TtlHours"] = "24",
                ["Cache:NegativeTtlSeconds"] = "60",
                ["Cache:ConnectTimeoutMs"] = "2000",
                ["Cache:OperationTimeoutMs"] = "2000",
            });
        });
    }

    public new async Task DisposeAsync()
    {
        await _postgres.DisposeAsync();
        await _redis.DisposeAsync();
    }
}
