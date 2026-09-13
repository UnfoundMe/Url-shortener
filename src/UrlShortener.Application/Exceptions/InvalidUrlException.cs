namespace UrlShortener.Application.Exceptions;

/// <summary>
/// Thrown when a submitted URL fails validation (empty, malformed, disallowed scheme, or too long).
/// </summary>
public sealed class InvalidUrlException : Exception
{
    public InvalidUrlException(string message) : base(message)
    {
    }
}
