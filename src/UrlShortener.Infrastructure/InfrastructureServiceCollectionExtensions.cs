using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using UrlShortener.Application.Abstractions;
using UrlShortener.Application.Options;
using UrlShortener.Infrastructure.Caching;
using UrlShortener.Infrastructure.HealthChecks;
using UrlShortener.Infrastructure.Persistence;
using UrlShortener.Infrastructure.ShortCodes;

namespace UrlShortener.Infrastructure;

public static class InfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<UrlShortenerDbContext>((sp, options) =>
        {
            var postgresConnectionString = sp.GetRequiredService<IConfiguration>().GetConnectionString("Postgres")
                ?? throw new InvalidOperationException("Missing required connection string 'ConnectionStrings:Postgres'.");

            options.UseNpgsql(postgresConnectionString);
        });

        services.AddSingleton<IConnectionMultiplexer>(sp =>
        {
            var logger = sp.GetRequiredService<ILoggerFactory>().CreateLogger("UrlShortener.Infrastructure.Redis");
            var cacheOptions = sp.GetRequiredService<IOptions<CacheOptions>>().Value;
            var redisConnectionString = configuration.GetConnectionString("Redis");

            if (string.IsNullOrWhiteSpace(redisConnectionString))
            {
                logger.LogWarning("No 'ConnectionStrings:Redis' configured; caching is disabled.");
                return null!;
            }

            try
            {
                var configurationOptions = ConfigurationOptions.Parse(redisConnectionString);
                configurationOptions.AbortOnConnectFail = false;
                configurationOptions.ConnectTimeout = cacheOptions.ConnectTimeoutMs;
                configurationOptions.SyncTimeout = cacheOptions.OperationTimeoutMs;
                configurationOptions.AsyncTimeout = cacheOptions.OperationTimeoutMs;
                configurationOptions.ConnectRetry = 1;

                return ConnectionMultiplexer.Connect(configurationOptions);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to establish initial Redis connection; caching will be disabled");
                return null!;
            }
        });

        services.AddScoped<IShortenedUrlRepository, EfShortenedUrlRepository>();
        services.AddSingleton<IShortCodeGenerator, Base62ShortCodeGenerator>();
        services.AddScoped<IShortUrlCache, RedisShortUrlCache>();

        services.AddHealthChecks()
            .AddCheck<PostgresHealthCheck>("postgres", tags: ["ready"])
            .AddCheck<RedisHealthCheck>("redis", tags: ["ready"]);

        return services;
    }
}
