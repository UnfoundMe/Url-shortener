namespace UrlShortener.Application.Exceptions;

/// <summary>
/// Thrown by <see cref="Abstractions.IShortenedUrlRepository.AddAsync"/> when the generated short code
/// collides with an existing one at the storage layer (e.g. a unique constraint violation).
/// </summary>
public sealed class ShortCodeCollisionException : Exception
{
    public string ShortCode { get; }

    public ShortCodeCollisionException(string shortCode, Exception innerException)
        : base($"Short code '{shortCode}' already exists.", innerException)
    {
        ShortCode = shortCode;
    }
}
