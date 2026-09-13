using Microsoft.Extensions.Diagnostics.HealthChecks;
using StackExchange.Redis;

namespace UrlShortener.Infrastructure.HealthChecks;

/// <summary>
/// Redis is a soft dependency: reports Degraded (not Unhealthy) on failure so the overall
/// readiness probe still passes and the service keeps serving redirects from PostgreSQL.
/// </summary>
public sealed class RedisHealthCheck : IHealthCheck
{
    private readonly IConnectionMultiplexer? _multiplexer;

    public RedisHealthCheck(IConnectionMultiplexer multiplexer)
    {
        _multiplexer = multiplexer;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        if (_multiplexer is null)
        {
            return HealthCheckResult.Degraded("Redis connection was not established; caching is disabled.");
        }

        try
        {
            var latency = await _multiplexer.GetDatabase().PingAsync();
            return HealthCheckResult.Healthy($"Redis responded in {latency.TotalMilliseconds:F0}ms.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Degraded("Redis is unreachable; falling back to PostgreSQL.", ex);
        }
    }
}
