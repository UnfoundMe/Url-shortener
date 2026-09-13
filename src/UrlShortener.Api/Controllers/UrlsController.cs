using Microsoft.AspNetCore.Mvc;
using UrlShortener.Api.Contracts;
using UrlShortener.Application.Services;

namespace UrlShortener.Api.Controllers;

[ApiController]
[Route("api/urls")]
[Produces("application/json")]
public sealed class UrlsController : ControllerBase
{
    private readonly IUrlShortenerService _urlShortenerService;

    public UrlsController(IUrlShortenerService urlShortenerService)
    {
        _urlShortenerService = urlShortenerService;
    }

    /// <summary>
    /// Creates a new shortened URL. Not idempotent: repeated requests for the same
    /// original URL generate different short codes.
    /// </summary>
    /// <response code="201">The short URL was created.</response>
    /// <response code="400">The submitted URL failed validation.</response>
    /// <response code="503">A unique short code could not be generated; retry later.</response>
    [HttpPost]
    [ProducesResponseType(typeof(CreateShortUrlResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<CreateShortUrlResponse>> CreateShortUrl(
        [FromBody] CreateShortUrlRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _urlShortenerService.CreateShortUrlAsync(request.Url, cancellationToken);

        var shortUrl = BuildShortUrl(result.ShortCode);
        var response = new CreateShortUrlResponse(result.ShortCode, shortUrl, result.OriginalUrl, result.CreatedAtUtc);

        return Created(shortUrl, response);
    }

    private string BuildShortUrl(string shortCode) =>
        $"{Request.Scheme}://{Request.Host}/{shortCode}";
}
