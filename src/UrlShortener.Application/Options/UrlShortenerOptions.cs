using System.ComponentModel.DataAnnotations;

namespace UrlShortener.Application.Options;

/// <summary>
/// Configuration governing short-code generation and redirect behavior.
/// Bound from the "UrlShortener" configuration section.
/// </summary>
public sealed class UrlShortenerOptions
{
    public const string SectionName = "UrlShortener";

    [Range(4, 32)]
    public int ShortCodeLength { get; set; } = 7;

    [Range(1, 20)]
    public int MaxCollisionRetries { get; set; } = 5;

    [Range(1, 8192)]
    public int MaxUrlLength { get; set; } = 2048;

    [Range(300, 399)]
    public int RedirectStatusCode { get; set; } = 302;
}
