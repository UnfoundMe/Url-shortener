namespace UrlShortener.Api.Contracts;

/// <summary>Response body for POST /api/urls.</summary>
public sealed record CreateShortUrlResponse(string ShortCode, string ShortUrl, string OriginalUrl, DateTime CreatedAtUtc);
