using UrlShortener.Application.Exceptions;
using UrlShortener.Application.Validation;

namespace UrlShortener.UnitTests.Application;

public class UrlValidatorTests
{
    [Theory]
    [InlineData("http://example.com")]
    [InlineData("https://example.com")]
    [InlineData("https://example.com/some/long/path?query=1&other=2")]
    [InlineData("HTTPS://EXAMPLE.COM")]
    public void EnsureValid_WithHttpOrHttpsUrl_DoesNotThrow(string url)
    {
        var exception = Record.Exception(() => UrlValidator.EnsureValid(url, 2048));

        Assert.Null(exception);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void EnsureValid_WithEmptyUrl_ThrowsInvalidUrlException(string? url)
    {
        Assert.Throws<InvalidUrlException>(() => UrlValidator.EnsureValid(url, 2048));
    }

    [Theory]
    [InlineData("ftp://example.com")]
    [InlineData("file:///etc/passwd")]
    [InlineData("javascript:alert(1)")]
    [InlineData("mailto:user@example.com")]
    public void EnsureValid_WithDisallowedScheme_ThrowsInvalidUrlException(string url)
    {
        Assert.Throws<InvalidUrlException>(() => UrlValidator.EnsureValid(url, 2048));
    }

    [Fact]
    public void EnsureValid_WithMalformedUrl_ThrowsInvalidUrlException()
    {
        Assert.Throws<InvalidUrlException>(() => UrlValidator.EnsureValid("not a url", 2048));
    }

    [Fact]
    public void EnsureValid_WithUrlExceedingMaxLength_ThrowsInvalidUrlException()
    {
        var longUrl = "https://example.com/" + new string('a', 2048);

        Assert.Throws<InvalidUrlException>(() => UrlValidator.EnsureValid(longUrl, 2048));
    }

    [Fact]
    public void EnsureValid_WithUrlAtExactMaxLength_DoesNotThrow()
    {
        var url = "https://example.com/" + new string('a', 2048 - "https://example.com/".Length);
        Assert.Equal(2048, url.Length);

        var exception = Record.Exception(() => UrlValidator.EnsureValid(url, 2048));

        Assert.Null(exception);
    }

    [Fact]
    public void EnsureValid_RespectsConfiguredMaxLength()
    {
        var url = "https://example.com/" + new string('a', 50);

        Assert.Throws<InvalidUrlException>(() => UrlValidator.EnsureValid(url, 10));
    }
}
