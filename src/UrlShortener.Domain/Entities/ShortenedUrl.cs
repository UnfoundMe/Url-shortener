namespace UrlShortener.Domain.Entities;

/// <summary>
/// A durable mapping between a short code and the original URL it redirects to.
/// </summary>
public sealed class ShortenedUrl
{
    public Guid Id { get; private set; }

    public string ShortCode { get; private set; }

    public string OriginalUrl { get; private set; }

    public DateTime CreatedAt { get; private set; }

    private ShortenedUrl(Guid id, string shortCode, string originalUrl, DateTime createdAt)
    {
        Id = id;
        ShortCode = shortCode;
        OriginalUrl = originalUrl;
        CreatedAt = createdAt;
    }

    public static ShortenedUrl Create(string shortCode, string originalUrl, DateTime createdAtUtc)
    {
        if (string.IsNullOrWhiteSpace(shortCode))
        {
            throw new ArgumentException("Short code must not be empty.", nameof(shortCode));
        }

        if (string.IsNullOrWhiteSpace(originalUrl))
        {
            throw new ArgumentException("Original URL must not be empty.", nameof(originalUrl));
        }

        if (createdAtUtc.Kind != DateTimeKind.Utc)
        {
            throw new ArgumentException("CreatedAt must be expressed in UTC.", nameof(createdAtUtc));
        }

        return new ShortenedUrl(Guid.NewGuid(), shortCode, originalUrl, createdAtUtc);
    }
}
