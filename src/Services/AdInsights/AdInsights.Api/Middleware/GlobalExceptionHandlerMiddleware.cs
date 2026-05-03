using System.Text.Json;
using AdInsights.Domain.Exceptions;
using FluentValidation;

namespace AdInsights.Api.Middleware;

/// <summary>
/// Global exception handler middleware that maps domain and validation exceptions
/// to standardised RFC 7807 Problem Details HTTP responses.
///
/// Mapping:
///   - <see cref="CampaignNotFoundException"/> → 404 Not Found
///   - <see cref="TenantAccessDeniedException"/> → 403 Forbidden
///   - <see cref="ValidationException"/> → 400 Bad Request (with field errors)
///   - <see cref="ArgumentException"/> → 400 Bad Request
///   - All others → 500 Internal Server Error (error detail hidden in production)
/// </summary>
public sealed class GlobalExceptionHandlerMiddleware : IMiddleware
{
    private readonly ILogger<GlobalExceptionHandlerMiddleware> _logger;
    private readonly IHostEnvironment _environment;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public GlobalExceptionHandlerMiddleware(
        ILogger<GlobalExceptionHandlerMiddleware> logger,
        IHostEnvironment environment)
    {
        _logger = logger;
        _environment = environment;
    }

    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        try
        {
            await next(context);
        }
        catch (CampaignNotFoundException ex)
        {
            _logger.LogWarning("Campaign not found: {Message}", ex.Message);
            await WriteErrorResponse(context, StatusCodes.Status404NotFound, ex.ErrorCode, ex.Message);
        }
        catch (TenantAccessDeniedException ex)
        {
            _logger.LogWarning("Tenant access denied: {Message}", ex.Message);
            await WriteErrorResponse(context, StatusCodes.Status403Forbidden, ex.ErrorCode, ex.Message);
        }
        catch (ValidationException ex)
        {
            _logger.LogInformation("Validation failed: {Errors}", string.Join("; ", ex.Errors.Select(e => e.ErrorMessage)));
            var errors = ex.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());
            await WriteValidationError(context, errors);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning("Argument error: {Message}", ex.Message);
            await WriteErrorResponse(context, StatusCodes.Status400BadRequest, "INVALID_ARGUMENT", ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception");

            var detail = _environment.IsDevelopment()
                ? ex.ToString()
                : "An unexpected error occurred.";

            await WriteErrorResponse(context, StatusCodes.Status500InternalServerError, "INTERNAL_ERROR", detail);
        }
    }

    private static Task WriteErrorResponse(HttpContext context, int statusCode, string errorCode, string detail)
    {
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/problem+json";

        var problem = new
        {
            type = $"https://adinsights.io/errors/{errorCode.ToLower()}",
            title = errorCode,
            status = statusCode,
            detail,
            traceId = context.TraceIdentifier
        };

        return context.Response.WriteAsync(JsonSerializer.Serialize(problem, JsonOptions));
    }

    private static Task WriteValidationError(HttpContext context, Dictionary<string, string[]> errors)
    {
        context.Response.StatusCode = StatusCodes.Status400BadRequest;
        context.Response.ContentType = "application/problem+json";

        var problem = new
        {
            type = "https://adinsights.io/errors/validation",
            title = "VALIDATION_FAILED",
            status = StatusCodes.Status400BadRequest,
            detail = "One or more validation errors occurred.",
            errors,
            traceId = context.TraceIdentifier
        };

        return context.Response.WriteAsync(JsonSerializer.Serialize(problem, JsonOptions));
    }
}
