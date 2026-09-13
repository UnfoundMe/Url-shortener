using Microsoft.Extensions.Diagnostics.HealthChecks;
using UrlShortener.Infrastructure.Persistence;

namespace UrlShortener.Infrastructure.HealthChecks;

/// <summary>
/// PostgreSQL is the source of truth: connectivity failures make the service unready.
/// </summary>
public sealed class PostgresHealthCheck : IHealthCheck
{
    private readonly UrlShortenerDbContext _dbContext;

    public PostgresHealthCheck(UrlShortenerDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var canConnect = await _dbContext.Database.CanConnectAsync(cancellationToken);
            return canConnect
                ? HealthCheckResult.Healthy("PostgreSQL is reachable.")
                : HealthCheckResult.Unhealthy("Cannot connect to PostgreSQL.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("PostgreSQL health check failed.", ex);
        }
    }
}
