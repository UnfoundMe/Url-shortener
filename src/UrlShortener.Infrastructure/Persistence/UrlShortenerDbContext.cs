using Microsoft.EntityFrameworkCore;
using UrlShortener.Domain.Entities;

namespace UrlShortener.Infrastructure.Persistence;

public sealed class UrlShortenerDbContext : DbContext
{
    public UrlShortenerDbContext(DbContextOptions<UrlShortenerDbContext> options) : base(options)
    {
    }

    public DbSet<ShortenedUrl> ShortenedUrls => Set<ShortenedUrl>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(UrlShortenerDbContext).Assembly);
    }
}
