using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using UrlShortener.Application.Abstractions;
using UrlShortener.Application.Dtos;
using UrlShortener.Application.Exceptions;
using UrlShortener.Application.Options;
using UrlShortener.Application.Validation;
using UrlShortener.Domain.Entities;

namespace UrlShortener.Application.Services;

public sealed class UrlShortenerService : IUrlShortenerService
{
    private readonly IShortenedUrlRepository _repository;
    private readonly IShortCodeGenerator _shortCodeGenerator;
    private readonly IShortUrlCache _cache;
    private readonly TimeProvider _timeProvider;
    private readonly UrlShortenerOptions _options;
    private readonly ILogger<UrlShortenerService> _logger;

    public UrlShortenerService(
        IShortenedUrlRepository repository,
        IShortCodeGenerator shortCodeGenerator,
        IShortUrlCache cache,
        TimeProvider timeProvider,
        IOptions<UrlShortenerOptions> options,
        ILogger<UrlShortenerService> logger)
    {
        _repository = repository;
        _shortCodeGenerator = shortCodeGenerator;
        _cache = cache;
        _timeProvider = timeProvider;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<CreateShortUrlResult> CreateShortUrlAsync(string originalUrl, CancellationToken cancellationToken)
    {
        UrlValidator.EnsureValid(originalUrl, _options.MaxUrlLength);

        for (var attempt = 1; attempt <= _options.MaxCollisionRetries; attempt++)
        {
            var candidate = _shortCodeGenerator.Generate(_options.ShortCodeLength);

            if (await _repository.ShortCodeExistsAsync(candidate, cancellationToken))
            {
                _logger.LogWarning("Short code collision detected on attempt {Attempt} for candidate {ShortCode}", attempt, candidate);
                continue;
            }

            var createdAtUtc = _timeProvider.GetUtcNow().UtcDateTime;
            var entity = ShortenedUrl.Create(candidate, originalUrl, createdAtUtc);

            try
            {
                await _repository.AddAsync(entity, cancellationToken);
            }
            catch (ShortCodeCollisionException)
            {
                _logger.LogWarning("Short code collision on insert for attempt {Attempt}, candidate {ShortCode}", attempt, candidate);
                continue;
            }

            await _cache.SetAsync(candidate, originalUrl, cancellationToken);

            _logger.LogInformation("Created short URL {ShortCode} after {Attempt} attempt(s)", candidate, attempt);
            return new CreateShortUrlResult(candidate, originalUrl, createdAtUtc);
        }

        _logger.LogError("Exhausted {MaxRetries} short code generation attempts", _options.MaxCollisionRetries);
        throw new ShortCodeGenerationFailedException(_options.MaxCollisionRetries);
    }

    public async Task<string?> ResolveOriginalUrlAsync(string shortCode, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(shortCode))
        {
            return null;
        }

        var cached = await _cache.GetAsync(shortCode, cancellationToken);
        switch (cached.Status)
        {
            case CacheLookupStatus.Hit:
                return cached.OriginalUrl;
            case CacheLookupStatus.NegativeHit:
                return null;
        }

        var entity = await _repository.GetByShortCodeAsync(shortCode, cancellationToken);
        if (entity is null)
        {
            await _cache.SetNotFoundAsync(shortCode, cancellationToken);
            return null;
        }

        await _cache.SetAsync(shortCode, entity.OriginalUrl, cancellationToken);
        return entity.OriginalUrl;
    }
}
