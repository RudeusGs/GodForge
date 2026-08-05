using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;

namespace GodForge.Api.Middleware;

public sealed class CorrelationIdMiddleware
{
    private const string CorrelationIdHeaderName = "X-Correlation-Id";
    private const int MaxCorrelationIdLength = 80;
    private readonly RequestDelegate _next;

    public CorrelationIdMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = GetCorrelationId(context);
        context.Items["CorrelationId"] = correlationId;

        context.Response.OnStarting(() =>
        {
            if (!context.Response.Headers.ContainsKey(CorrelationIdHeaderName))
                context.Response.Headers.Append(CorrelationIdHeaderName, correlationId);

            return Task.CompletedTask;
        });

        await _next(context);
    }

    private static string GetCorrelationId(HttpContext context)
    {
        if (!context.Request.Headers.TryGetValue(CorrelationIdHeaderName, out StringValues values) || values.Count != 1)
            return CreateCorrelationId();

        var candidate = values[0]?.Trim();
        if (string.IsNullOrWhiteSpace(candidate) || candidate.Length > MaxCorrelationIdLength)
            return CreateCorrelationId();

        foreach (var character in candidate)
        {
            if (!char.IsAsciiLetterOrDigit(character) && character is not '.' and not '_' and not ':' and not '-')
                return CreateCorrelationId();
        }

        return candidate;
    }

    private static string CreateCorrelationId() => Guid.NewGuid().ToString("N");
}
