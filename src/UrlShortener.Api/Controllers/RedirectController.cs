using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using UrlShortener.Application.Options;
using UrlShortener.Application.Services;

namespace UrlShortener.Api.Controllers;

[ApiController]
public sealed class RedirectController : ControllerBase
{
    private readonly IUrlShortenerService _urlShortenerService;
    private readonly UrlShortenerOptions _options;

    public RedirectController(IUrlShortenerService urlShortenerService, IOptions<UrlShortenerOptions> options)
    {
        _urlShortenerService = urlShortenerService;
        _options = options.Value;
    }

    /// <summary>
    /// Redirects a short code to its original URL. PostgreSQL is checked via Redis first (cache-aside);
    /// Redis unavailability falls back transparently to PostgreSQL.
    /// </summary>
    /// <response code="302">Redirects to the original URL (status code is configurable).</response>
    /// <response code="404">The short code does not exist.</response>
    [HttpGet("/{shortCode:regex(^[[0-9A-Za-z]]{{1,32}}$)}")]
    [ProducesResponseType(StatusCodes.Status302Found)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RedirectToOriginalUrl(string shortCode, CancellationToken cancellationToken)
    {
        var originalUrl = await _urlShortenerService.ResolveOriginalUrlAsync(shortCode, cancellationToken);

        if (originalUrl is null)
        {
            return NotFound(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Short code not found",
                Detail = $"No URL was found for short code '{shortCode}'.",
            });
        }

        Response.Headers.Location = originalUrl;
        return StatusCode(_options.RedirectStatusCode);
    }
}
