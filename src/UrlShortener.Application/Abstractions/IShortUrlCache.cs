namespace UrlShortener.Application.Abstractions;

public enum CacheLookupStatus
{
    /// <summary>The short code was found in the cache.</summary>
    Hit,

    /// <summary>The short code is cached as known not to exist (negative cache).</summary>
    NegativeHit,

    /// <summary>The short code was not in the cache, or the cache is unavailable.</summary>
    Miss,
}

public readonly record struct CachedUrlLookup(CacheLookupStatus Status, string? OriginalUrl)
{
    public static CachedUrlLookup Hit(string originalUrl) => new(CacheLookupStatus.Hit, originalUrl);

    public static readonly CachedUrlLookup NegativeHit = new(CacheLookupStatus.NegativeHit, null);

    public static readonly CachedUrlLookup Miss = new(CacheLookupStatus.Miss, null);
}

/// <summary>
/// Cache-aside abstraction over the short-code -&gt; original-URL mapping.
/// Implementations must be resilient to the underlying cache being unavailable:
/// failures should be caught internally and surfaced as <see cref="CacheLookupStatus.Miss"/>,
/// never thrown, so callers can always fall back to the durable store.
/// </summary>
public interface IShortUrlCache
{
    Task<CachedUrlLookup> GetAsync(string shortCode, CancellationToken cancellationToken);

    Task SetAsync(string shortCode, string originalUrl, CancellationToken cancellationToken);

    Task SetNotFoundAsync(string shortCode, CancellationToken cancellationToken);
}
