namespace UrlShortener.Application.Abstractions;

/// <summary>
/// Generates unpredictable Base62 short codes using a cryptographically secure random source.
/// </summary>
public interface IShortCodeGenerator
{
    string Generate(int length);
}
