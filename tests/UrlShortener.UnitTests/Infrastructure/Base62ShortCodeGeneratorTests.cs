using UrlShortener.Infrastructure.ShortCodes;

namespace UrlShortener.UnitTests.Infrastructure;

public class Base62ShortCodeGeneratorTests
{
    private static readonly HashSet<char> Base62Alphabet =
        "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz".ToHashSet();

    private readonly Base62ShortCodeGenerator _generator = new();

    [Theory]
    [InlineData(1)]
    [InlineData(7)]
    [InlineData(16)]
    [InlineData(32)]
    public void Generate_ReturnsCodeOfConfiguredLength(int length)
    {
        var code = _generator.Generate(length);

        Assert.Equal(length, code.Length);
    }

    [Fact]
    public void Generate_OnlyUsesBase62Characters()
    {
        var code = _generator.Generate(200);

        Assert.All(code, c => Assert.Contains(c, Base62Alphabet));
    }

    [Fact]
    public void Generate_WithZeroLength_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => _generator.Generate(0));
    }

    [Fact]
    public void Generate_WithNegativeLength_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => _generator.Generate(-1));
    }

    [Fact]
    public void Generate_ProducesDifferentCodesAcrossCalls()
    {
        var codes = Enumerable.Range(0, 100).Select(_ => _generator.Generate(7)).ToHashSet();

        // Cryptographically random 7-char Base62 codes should essentially never collide across 100 samples.
        Assert.True(codes.Count > 95, $"Expected near-unique codes, got {codes.Count} distinct out of 100.");
    }
}
