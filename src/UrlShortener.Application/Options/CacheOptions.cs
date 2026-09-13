using System.ComponentModel.DataAnnotations;

namespace UrlShortener.Application.Options;

/// <summary>
/// Configuration governing the Redis cache-aside layer.
/// Bound from the "Cache" configuration section.
/// </summary>
public sealed class CacheOptions
{
    public const string SectionName = "Cache";

    [Range(1, 24 * 30)]
    public int TtlHours { get; set; } = 24;

    [Range(1, 3600)]
    public int NegativeTtlSeconds { get; set; } = 60;

    [Range(50, 30000)]
    public int ConnectTimeoutMs { get; set; } = 1000;

    [Range(50, 30000)]
    public int OperationTimeoutMs { get; set; } = 500;
}
