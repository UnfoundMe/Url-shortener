namespace UrlShortener.Application.Exceptions;

/// <summary>
/// Thrown when no unique short code could be produced within the configured retry budget.
/// </summary>
public sealed class ShortCodeGenerationFailedException : Exception
{
    public ShortCodeGenerationFailedException(int attempts)
        : base($"Failed to generate a unique short code after {attempts} attempt(s).")
    {
    }
}
