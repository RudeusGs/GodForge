using GodForge.Api.Services;
using GodForge.Application.Common.Models;

namespace GodForge.Api.Middleware;

public sealed class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception exception) when (!context.Response.HasStarted)
        {
            var mapping = Map(exception);
            if (mapping.StatusCode >= StatusCodes.Status500InternalServerError)
                _logger.LogError(exception, "An unhandled exception occurred. CorrelationId: {CorrelationId}", ApiErrorResponseFactory.GetCorrelationId(context));
            else
                _logger.LogWarning(exception, "Request failed with {ErrorCode}. CorrelationId: {CorrelationId}", mapping.Code, ApiErrorResponseFactory.GetCorrelationId(context));

            context.Response.Clear();
            await ApiErrorResponseWriter.WriteAsync(
                context,
                mapping.StatusCode,
                mapping.Code,
                mapping.Message,
                context.RequestAborted,
                mapping.Details);
        }
    }

    private static ExceptionMapping Map(Exception exception) => exception switch
    {
        UnauthorizedAccessException => new(
            StatusCodes.Status401Unauthorized,
            "UNAUTHORIZED",
            "Authentication is missing or invalid."),
        ConcurrencyConflictException => new(
            StatusCodes.Status409Conflict,
            "CONCURRENCY_CONFLICT",
            "The resource changed before this operation completed."),
        FluentValidation.ValidationException validationException => new(
            StatusCodes.Status400BadRequest,
            "VALIDATION_ERROR",
            "Request validation failed.",
            validationException.Errors
                .GroupBy(failure => failure.PropertyName, StringComparer.Ordinal)
                .ToDictionary(
                    group => group.Key,
                    group => group.Select(failure => failure.ErrorMessage)
                        .Distinct(StringComparer.Ordinal)
                        .ToArray(),
                    StringComparer.Ordinal)),
        BadHttpRequestException => new(
            StatusCodes.Status400BadRequest,
            "VALIDATION_ERROR",
            "The request is invalid."),
        _ => new(
            StatusCodes.Status500InternalServerError,
            "INTERNAL_SERVER_ERROR",
            "An unexpected error occurred.")
    };

    private sealed record ExceptionMapping(int StatusCode, string Code, string Message, object? Details = null);
}
