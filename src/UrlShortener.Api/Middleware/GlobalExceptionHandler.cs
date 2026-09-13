using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using UrlShortener.Application.Exceptions;

namespace UrlShortener.Api.Middleware;

/// <summary>
/// Maps unhandled exceptions to a consistent RFC 7807 <see cref="ProblemDetails"/> response.
/// Internal details (stack traces, connection strings, etc.) are never included in the response
/// body; the full exception is only written to the server-side log.
/// </summary>
public sealed class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var (statusCode, title, detail) = MapException(exception);

        if (statusCode == StatusCodes.Status500InternalServerError)
        {
            _logger.LogError(exception, "Unhandled exception processing {Method} {Path}", httpContext.Request.Method, httpContext.Request.Path);
        }
        else
        {
            _logger.LogWarning(exception, "Request {Method} {Path} failed with status {StatusCode}", httpContext.Request.Method, httpContext.Request.Path, statusCode);
        }

        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = detail,
            Instance = httpContext.Request.Path,
        };

        httpContext.Response.StatusCode = statusCode;
        httpContext.Response.ContentType = "application/problem+json";
        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);

        return true;
    }

    private static (int StatusCode, string Title, string Detail) MapException(Exception exception) => exception switch
    {
        InvalidUrlException ex => (StatusCodes.Status400BadRequest, "Invalid URL", ex.Message),
        ShortCodeGenerationFailedException => (
            StatusCodes.Status503ServiceUnavailable,
            "Short code generation failed",
            "Unable to generate a unique short code at this time. Please try again."),
        _ => (
            StatusCodes.Status500InternalServerError,
            "An unexpected error occurred",
            "An unexpected error occurred while processing your request."),
    };
}
