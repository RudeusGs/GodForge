using System.Text.Json;
using GodForge.Api.Middleware;
using GodForge.Application.Common.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;

namespace GodForge.UnitTests.Api.Middleware;

public sealed class ExceptionHandlingMiddlewareTests
{
    [Fact]
    public async Task InvokeAsync_UnauthorizedAccessException_ReturnsStandardUnauthorizedEnvelope()
    {
        var context = CreateContext();
        var middleware = CreateMiddleware(new UnauthorizedAccessException("missing claim"));

        await middleware.InvokeAsync(context);

        Assert.Equal(StatusCodes.Status401Unauthorized, context.Response.StatusCode);
        using var body = await ReadBodyAsync(context);
        Assert.Equal("UNAUTHORIZED", body.RootElement.GetProperty("error").GetProperty("code").GetString());
        Assert.Equal(
            "correlation-123",
            body.RootElement.GetProperty("error").GetProperty("correlationId").GetString());
    }

    [Fact]
    public async Task InvokeAsync_ConcurrencyConflictException_ReturnsConflictWithoutLeakingException()
    {
        var context = CreateContext();
        var middleware = CreateMiddleware(new ConcurrencyConflictException("database details"));

        await middleware.InvokeAsync(context);

        Assert.Equal(StatusCodes.Status409Conflict, context.Response.StatusCode);
        using var body = await ReadBodyAsync(context);
        var error = body.RootElement.GetProperty("error");
        Assert.Equal("CONCURRENCY_CONFLICT", error.GetProperty("code").GetString());
        var message = Assert.IsType<string>(error.GetProperty("message").GetString());
        Assert.DoesNotContain("database details", message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task InvokeAsync_UnhandledException_ReturnsSanitizedInternalServerError()
    {
        var context = CreateContext();
        var middleware = CreateMiddleware(new InvalidOperationException("sensitive implementation detail"));

        await middleware.InvokeAsync(context);

        Assert.Equal(StatusCodes.Status500InternalServerError, context.Response.StatusCode);
        using var body = await ReadBodyAsync(context);
        var error = body.RootElement.GetProperty("error");
        Assert.Equal("INTERNAL_SERVER_ERROR", error.GetProperty("code").GetString());
        var message = Assert.IsType<string>(error.GetProperty("message").GetString());
        Assert.DoesNotContain("sensitive", message, StringComparison.OrdinalIgnoreCase);
    }

    private static ExceptionHandlingMiddleware CreateMiddleware(Exception exception)
        => new(
            _ => Task.FromException(exception),
            NullLogger<ExceptionHandlingMiddleware>.Instance);

    private static DefaultHttpContext CreateContext()
    {
        var context = new DefaultHttpContext();
        context.Items["CorrelationId"] = "correlation-123";
        context.Response.Body = new MemoryStream();
        return context;
    }

    private static async Task<JsonDocument> ReadBodyAsync(HttpContext context)
    {
        context.Response.Body.Position = 0;
        return await JsonDocument.ParseAsync(context.Response.Body);
    }
}
