using GodForge.Api.Controllers;

namespace GodForge.Api.Services;

public static class ApiErrorResponseFactory
{
    public static ApiErrorResponse Create(
        HttpContext context,
        string code,
        string message,
        object? details = null)
        => new()
        {
            Error = new ApiErrorDetail
            {
                Code = code,
                Message = message,
                CorrelationId = GetCorrelationId(context),
                Details = details
            }
        };

    public static string GetCorrelationId(HttpContext context)
    {
        var correlationId = context.Items["CorrelationId"]?.ToString();
        return string.IsNullOrWhiteSpace(correlationId)
            ? context.TraceIdentifier
            : correlationId;
    }
}
