using System.ComponentModel.DataAnnotations;

namespace UrlShortener.Api.Contracts;

/// <summary>Request body for POST /api/urls.</summary>
public sealed record CreateShortUrlRequest([Required] string Url);
