using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UrlShortener.Domain.Entities;

namespace UrlShortener.Infrastructure.Persistence.Configurations;

public sealed class ShortenedUrlConfiguration : IEntityTypeConfiguration<ShortenedUrl>
{
    public void Configure(EntityTypeBuilder<ShortenedUrl> builder)
    {
        builder.ToTable("short_urls");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(x => x.ShortCode)
            .HasColumnName("short_code")
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(x => x.OriginalUrl)
            .HasColumnName("original_url")
            .HasColumnType("text")
            .IsRequired();

        builder.Property(x => x.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        // Unique index: the primary access pattern is an exact-match lookup by short code on every redirect.
        builder.HasIndex(x => x.ShortCode)
            .IsUnique()
            .HasDatabaseName("ix_short_urls_short_code");

        // original_url is intentionally NOT unique/indexed: URLs are not deduplicated.
    }
}
