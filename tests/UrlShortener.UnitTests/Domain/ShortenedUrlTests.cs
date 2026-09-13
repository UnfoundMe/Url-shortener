using UrlShortener.Domain.Entities;

namespace UrlShortener.UnitTests.Domain;

public class ShortenedUrlTests
{
    [Fact]
    public void Create_WithValidInputs_PopulatesAllProperties()
    {
        var createdAt = DateTime.UtcNow;

        var entity = ShortenedUrl.Create("abc1234", "https://example.com", createdAt);

        Assert.NotEqual(Guid.Empty, entity.Id);
        Assert.Equal("abc1234", entity.ShortCode);
        Assert.Equal("https://example.com", entity.OriginalUrl);
        Assert.Equal(createdAt, entity.CreatedAt);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithEmptyShortCode_Throws(string? shortCode)
    {
        Assert.Throws<ArgumentException>(() => ShortenedUrl.Create(shortCode!, "https://example.com", DateTime.UtcNow));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithEmptyOriginalUrl_Throws(string? originalUrl)
    {
        Assert.Throws<ArgumentException>(() => ShortenedUrl.Create("abc1234", originalUrl!, DateTime.UtcNow));
    }

    [Fact]
    public void Create_WithNonUtcCreatedAt_Throws()
    {
        var localTime = DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Local);

        Assert.Throws<ArgumentException>(() => ShortenedUrl.Create("abc1234", "https://example.com", localTime));
    }

    [Fact]
    public void Create_GeneratesUniqueIdsAcrossInstances()
    {
        var first = ShortenedUrl.Create("abc1234", "https://example.com", DateTime.UtcNow);
        var second = ShortenedUrl.Create("xyz9876", "https://example.com", DateTime.UtcNow);

        Assert.NotEqual(first.Id, second.Id);
    }
}
