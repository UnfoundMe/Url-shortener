using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using UrlShortener.Application.Abstractions;
using UrlShortener.Application.Exceptions;
using UrlShortener.Application.Options;
using UrlShortener.Application.Services;
using UrlShortener.Domain.Entities;

namespace UrlShortener.UnitTests.Application;

public class UrlShortenerServiceTests
{
    private readonly Mock<IShortenedUrlRepository> _repository = new();
    private readonly Mock<IShortCodeGenerator> _generator = new();
    private readonly Mock<IShortUrlCache> _cache = new();
    private readonly FixedTimeProvider _timeProvider = new(new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero));
    private readonly UrlShortenerOptions _options = new() { ShortCodeLength = 7, MaxCollisionRetries = 5, MaxUrlLength = 2048 };

    private UrlShortenerService CreateSut() => new(
        _repository.Object,
        _generator.Object,
        _cache.Object,
        _timeProvider,
        Options.Create(_options),
        NullLogger<UrlShortenerService>.Instance);

    [Fact]
    public async Task CreateShortUrlAsync_WithInvalidUrl_ThrowsWithoutTouchingRepository()
    {
        var sut = CreateSut();

        await Assert.ThrowsAsync<InvalidUrlException>(() => sut.CreateShortUrlAsync("not-a-url", CancellationToken.None));

        _repository.Verify(r => r.AddAsync(It.IsAny<ShortenedUrl>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateShortUrlAsync_OnFirstAttemptSuccess_ReturnsResultAndPopulatesCache()
    {
        _generator.Setup(g => g.Generate(7)).Returns("abc1234");
        _repository.Setup(r => r.ShortCodeExistsAsync("abc1234", It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var sut = CreateSut();

        var result = await sut.CreateShortUrlAsync("https://example.com", CancellationToken.None);

        Assert.Equal("abc1234", result.ShortCode);
        Assert.Equal("https://example.com", result.OriginalUrl);
        _repository.Verify(r => r.AddAsync(It.Is<ShortenedUrl>(u => u.ShortCode == "abc1234"), It.IsAny<CancellationToken>()), Times.Once);
        _cache.Verify(c => c.SetAsync("abc1234", "https://example.com", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateShortUrlAsync_WhenExistsCheckCollides_RetriesUntilUniqueCode()
    {
        var sequence = new Queue<string>(new[] { "aaa1111", "aaa1111", "bbb2222" });
        _generator.Setup(g => g.Generate(7)).Returns(() => sequence.Dequeue());
        _repository.Setup(r => r.ShortCodeExistsAsync("aaa1111", It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _repository.Setup(r => r.ShortCodeExistsAsync("bbb2222", It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var sut = CreateSut();

        var result = await sut.CreateShortUrlAsync("https://example.com", CancellationToken.None);

        Assert.Equal("bbb2222", result.ShortCode);
        _generator.Verify(g => g.Generate(7), Times.Exactly(3));
        _repository.Verify(r => r.AddAsync(It.IsAny<ShortenedUrl>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateShortUrlAsync_WhenAddThrowsCollision_RetriesUntilSuccess()
    {
        var sequence = new Queue<string>(new[] { "aaa1111", "bbb2222" });
        _generator.Setup(g => g.Generate(7)).Returns(() => sequence.Dequeue());
        _repository.Setup(r => r.ShortCodeExistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _repository.Setup(r => r.AddAsync(It.Is<ShortenedUrl>(u => u.ShortCode == "aaa1111"), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ShortCodeCollisionException("aaa1111", new InvalidOperationException()));
        _repository.Setup(r => r.AddAsync(It.Is<ShortenedUrl>(u => u.ShortCode == "bbb2222"), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var sut = CreateSut();

        var result = await sut.CreateShortUrlAsync("https://example.com", CancellationToken.None);

        Assert.Equal("bbb2222", result.ShortCode);
        _generator.Verify(g => g.Generate(7), Times.Exactly(2));
    }

    [Fact]
    public async Task CreateShortUrlAsync_WhenAllAttemptsCollide_ThrowsAfterMaxRetries()
    {
        _generator.Setup(g => g.Generate(7)).Returns("aaa1111");
        _repository.Setup(r => r.ShortCodeExistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var sut = CreateSut();

        await Assert.ThrowsAsync<ShortCodeGenerationFailedException>(() => sut.CreateShortUrlAsync("https://example.com", CancellationToken.None));

        _generator.Verify(g => g.Generate(7), Times.Exactly(_options.MaxCollisionRetries));
        _repository.Verify(r => r.AddAsync(It.IsAny<ShortenedUrl>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ResolveOriginalUrlAsync_WithCacheHit_ReturnsCachedValueWithoutQueryingRepository()
    {
        _cache.Setup(c => c.GetAsync("abc1234", It.IsAny<CancellationToken>()))
            .ReturnsAsync(CachedUrlLookup.Hit("https://example.com"));

        var sut = CreateSut();

        var result = await sut.ResolveOriginalUrlAsync("abc1234", CancellationToken.None);

        Assert.Equal("https://example.com", result);
        _repository.Verify(r => r.GetByShortCodeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ResolveOriginalUrlAsync_WithCacheMiss_FallsBackToRepositoryAndPopulatesCache()
    {
        _cache.Setup(c => c.GetAsync("abc1234", It.IsAny<CancellationToken>())).ReturnsAsync(CachedUrlLookup.Miss);
        _repository.Setup(r => r.GetByShortCodeAsync("abc1234", It.IsAny<CancellationToken>()))
            .ReturnsAsync(ShortenedUrl.Create("abc1234", "https://example.com", DateTime.UtcNow));

        var sut = CreateSut();

        var result = await sut.ResolveOriginalUrlAsync("abc1234", CancellationToken.None);

        Assert.Equal("https://example.com", result);
        _cache.Verify(c => c.SetAsync("abc1234", "https://example.com", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ResolveOriginalUrlAsync_WithNegativeCacheHit_ReturnsNullWithoutQueryingRepository()
    {
        _cache.Setup(c => c.GetAsync("missing", It.IsAny<CancellationToken>())).ReturnsAsync(CachedUrlLookup.NegativeHit);

        var sut = CreateSut();

        var result = await sut.ResolveOriginalUrlAsync("missing", CancellationToken.None);

        Assert.Null(result);
        _repository.Verify(r => r.GetByShortCodeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ResolveOriginalUrlAsync_WhenNotFoundAnywhere_ReturnsNullAndCachesNegativeResult()
    {
        _cache.Setup(c => c.GetAsync("missing", It.IsAny<CancellationToken>())).ReturnsAsync(CachedUrlLookup.Miss);
        _repository.Setup(r => r.GetByShortCodeAsync("missing", It.IsAny<CancellationToken>()))
            .ReturnsAsync((ShortenedUrl?)null);

        var sut = CreateSut();

        var result = await sut.ResolveOriginalUrlAsync("missing", CancellationToken.None);

        Assert.Null(result);
        _cache.Verify(c => c.SetNotFoundAsync("missing", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task ResolveOriginalUrlAsync_WithEmptyShortCode_ReturnsNullWithoutTouchingDependencies(string? shortCode)
    {
        var sut = CreateSut();

        var result = await sut.ResolveOriginalUrlAsync(shortCode!, CancellationToken.None);

        Assert.Null(result);
        _cache.Verify(c => c.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        _repository.Verify(r => r.GetByShortCodeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
