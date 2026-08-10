namespace GodForge.Api.Services;

public static class ApiErrorResponseWriter
{
    public static Task WriteAsync(
        HttpContext context,
        int statusCode,
        string code,
        string message,
        CancellationToken cancellationToken = default,
        object? details = null)
    {
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json";
        return context.Response.WriteAsJsonAsync(
            ApiErrorResponseFactory.Create(context, code, message, details),
            cancellationToken);
    }
}
