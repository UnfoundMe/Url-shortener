using UrlShortener.Application.Exceptions;

namespace UrlShortener.Application.Validation;

/// <summary>
/// Validates user-submitted URLs before they are persisted. Only HTTP/HTTPS absolute URIs are accepted.
/// </summary>
public static class UrlValidator
{
    public static void EnsureValid(string? url, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            throw new InvalidUrlException("URL must not be empty.");
        }

        if (url.Length > maxLength)
        {
            throw new InvalidUrlException($"URL exceeds the maximum allowed length of {maxLength} characters.");
        }

        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
            throw new InvalidUrlException("URL is not a well-formed absolute URI.");
        }

        if (!uri.Scheme.Equals(Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase) &&
            !uri.Scheme.Equals(Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidUrlException("Only HTTP and HTTPS URLs are allowed.");
        }
    }
}
