namespace GodForge.Api.Services;

public static class ApiErrorResponseWriter
{
    public static Task WriteAsync(
        HttpContext context,
        int statusCode,
        string code,
        string message,
        CancellationToken cancellationToken = default)
    {
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json";
        var correlationId = context.Items["CorrelationId"]?.ToString();
        if (string.IsNullOrWhiteSpace(correlationId))
            correlationId = context.TraceIdentifier;

        return context.Response.WriteAsJsonAsync(new
        {
            error = new
            {
                code,
                message,
                correlationId
            }
        }, cancellationToken);
    }
}
