using Microsoft.EntityFrameworkCore;
using Npgsql;
using UrlShortener.Application.Abstractions;
using UrlShortener.Application.Exceptions;
using UrlShortener.Domain.Entities;

namespace UrlShortener.Infrastructure.Persistence;

public sealed class EfShortenedUrlRepository : IShortenedUrlRepository
{
    private readonly UrlShortenerDbContext _dbContext;

    public EfShortenedUrlRepository(UrlShortenerDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(ShortenedUrl shortenedUrl, CancellationToken cancellationToken)
    {
        _dbContext.ShortenedUrls.Add(shortenedUrl);

        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (IsUniqueViolation(ex))
        {
            _dbContext.Entry(shortenedUrl).State = EntityState.Detached;
            throw new ShortCodeCollisionException(shortenedUrl.ShortCode, ex);
        }
    }

    public Task<ShortenedUrl?> GetByShortCodeAsync(string shortCode, CancellationToken cancellationToken) =>
        _dbContext.ShortenedUrls
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.ShortCode == shortCode, cancellationToken);

    public Task<bool> ShortCodeExistsAsync(string shortCode, CancellationToken cancellationToken) =>
        _dbContext.ShortenedUrls
            .AsNoTracking()
            .AnyAsync(x => x.ShortCode == shortCode, cancellationToken);

    private static bool IsUniqueViolation(DbUpdateException ex) =>
        ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation };
}
