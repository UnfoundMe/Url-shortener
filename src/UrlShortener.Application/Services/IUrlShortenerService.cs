using UrlShortener.Application.Dtos;

namespace UrlShortener.Application.Services;

public interface IUrlShortenerService
{
    /// <summary>
    /// Validates and shortens a URL, generating a new, unique short code.
    /// Not idempotent: repeated calls with the same URL produce different short codes.
    /// </summary>
    /// <exception cref="Exceptions.InvalidUrlException">The URL failed validation.</exception>
    /// <exception cref="Exceptions.ShortCodeGenerationFailedException">
    /// No unique short code could be generated within the configured retry budget.
    /// </exception>
    Task<CreateShortUrlResult> CreateShortUrlAsync(string originalUrl, CancellationToken cancellationToken);

    /// <summary>
    /// Resolves a short code to its original URL, checking the cache before falling back to the
    /// durable store. Returns <c>null</c> when the short code does not exist.
    /// </summary>
    Task<string?> ResolveOriginalUrlAsync(string shortCode, CancellationToken cancellationToken);
}
