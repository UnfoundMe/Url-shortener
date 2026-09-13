using System.Security.Cryptography;
using UrlShortener.Application.Abstractions;

namespace UrlShortener.Infrastructure.ShortCodes;

/// <summary>
/// Generates unpredictable short codes from a cryptographically secure random source, using
/// rejection sampling to avoid modulo bias across the 62-character Base62 alphabet.
/// </summary>
public sealed class Base62ShortCodeGenerator : IShortCodeGenerator
{
    private const string Alphabet = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";

    // Largest multiple of 62 that fits in a byte, used to discard biased samples.
    private static readonly int UnbiasedUpperBound = 256 - (256 % Alphabet.Length);

    public string Generate(int length)
    {
        if (length <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(length), "Short code length must be positive.");
        }

        Span<char> buffer = length <= 128 ? stackalloc char[length] : new char[length];
        Span<byte> sample = stackalloc byte[1];

        for (var i = 0; i < length; i++)
        {
            byte value;
            do
            {
                RandomNumberGenerator.Fill(sample);
                value = sample[0];
            } while (value >= UnbiasedUpperBound);

            buffer[i] = Alphabet[value % Alphabet.Length];
        }

        return new string(buffer);
    }
}
