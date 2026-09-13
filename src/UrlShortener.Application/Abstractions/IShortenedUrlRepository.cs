using UrlShortener.Domain.Entities;

namespace UrlShortener.Application.Abstractions;

/// <summary>
/// Persistence abstraction over the durable store of shortened URLs (PostgreSQL is the source of truth).
/// </summary>
public interface IShortenedUrlRepository
{
    /// <summary>
    /// Persists a new shortened URL. Throws <see cref="Exceptions.ShortCodeCollisionException"/>
    /// if the short code is already taken (unique constraint violation).
    /// </summary>
    Task AddAsync(ShortenedUrl shortenedUrl, CancellationToken cancellationToken);

    Task<ShortenedUrl?> GetByShortCodeAsync(string shortCode, CancellationToken cancellationToken);

    Task<bool> ShortCodeExistsAsync(string shortCode, CancellationToken cancellationToken);
}
