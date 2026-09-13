namespace UrlShortener.Application.Dtos;

public sealed record CreateShortUrlResult(string ShortCode, string OriginalUrl, DateTime CreatedAtUtc);
