using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using UrlShortener.Application.Abstractions;
using UrlShortener.Application.Options;

namespace UrlShortener.Infrastructure.Caching;

/// <summary>
/// Cache-aside Redis implementation of <see cref="IShortUrlCache"/>.
/// Redis is a soft dependency: every operation catches Redis-specific failures internally and
/// degrades to a cache miss rather than throwing, so callers can always fall back to PostgreSQL.
/// </summary>
public sealed class RedisShortUrlCache : IShortUrlCache
{
    private const string NotFoundSentinel = "\0__NOT_FOUND__";
    private const string KeyPrefix = "shorturl:";

    private readonly IConnectionMultiplexer? _multiplexer;
    private readonly CacheOptions _options;
    private readonly ILogger<RedisShortUrlCache> _logger;

    public RedisShortUrlCache(IConnectionMultiplexer multiplexer, IOptions<CacheOptions> options, ILogger<RedisShortUrlCache> logger)
    {
        _multiplexer = multiplexer;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<CachedUrlLookup> GetAsync(string shortCode, CancellationToken cancellationToken)
    {
        if (_multiplexer is null || !_multiplexer.IsConnected)
        {
            return CachedUrlLookup.Miss;
        }

        try
        {
            var database = _multiplexer.GetDatabase();
            var value = await database.StringGetAsync(BuildKey(shortCode));

            if (value.IsNullOrEmpty)
            {
                return CachedUrlLookup.Miss;
            }

            var stringValue = value.ToString();
            return stringValue == NotFoundSentinel
                ? CachedUrlLookup.NegativeHit
                : CachedUrlLookup.Hit(stringValue);
        }
        catch (Exception ex) when (IsTransientRedisFailure(ex))
        {
            _logger.LogWarning(ex, "Redis GET failed for short code {ShortCode}; falling back to database", shortCode);
            return CachedUrlLookup.Miss;
        }
    }

    public async Task SetAsync(string shortCode, string originalUrl, CancellationToken cancellationToken)
    {
        if (_multiplexer is null || !_multiplexer.IsConnected)
        {
            return;
        }

        try
        {
            var database = _multiplexer.GetDatabase();
            await database.StringSetAsync(BuildKey(shortCode), originalUrl, TimeSpan.FromHours(_options.TtlHours));
        }
        catch (Exception ex) when (IsTransientRedisFailure(ex))
        {
            _logger.LogWarning(ex, "Redis SET failed for short code {ShortCode}; continuing without caching", shortCode);
        }
    }

    public async Task SetNotFoundAsync(string shortCode, CancellationToken cancellationToken)
    {
        if (_multiplexer is null || !_multiplexer.IsConnected)
        {
            return;
        }

        try
        {
            var database = _multiplexer.GetDatabase();
            await database.StringSetAsync(BuildKey(shortCode), NotFoundSentinel, TimeSpan.FromSeconds(_options.NegativeTtlSeconds));
        }
        catch (Exception ex) when (IsTransientRedisFailure(ex))
        {
            _logger.LogWarning(ex, "Redis negative-cache SET failed for short code {ShortCode}", shortCode);
        }
    }

    private static string BuildKey(string shortCode) => $"{KeyPrefix}{shortCode}";

    private static bool IsTransientRedisFailure(Exception ex) =>
        ex is RedisException or TimeoutException;
}
