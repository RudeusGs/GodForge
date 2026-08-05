using GodForge.Api.Middleware;
using Microsoft.AspNetCore.Http;

namespace GodForge.UnitTests.Api.Middleware;

public sealed class CorrelationIdMiddlewareTests
{
    [Fact]
    public async Task InvokeAsync_WithValidCorrelationId_PreservesValue()
    {
        var context = new DefaultHttpContext();
        context.Request.Headers["X-Correlation-Id"] = "request-123:_test";
        var middleware = new CorrelationIdMiddleware(_ => Task.CompletedTask);

        await middleware.InvokeAsync(context);

        Assert.Equal("request-123:_test", context.Items["CorrelationId"]);
    }

    [Theory]
    [InlineData("invalid value")]
    [InlineData("<script>")]
    public async Task InvokeAsync_WithUnsafeCorrelationId_GeneratesServerValue(string value)
    {
        var context = new DefaultHttpContext();
        context.Request.Headers["X-Correlation-Id"] = value;
        var middleware = new CorrelationIdMiddleware(_ => Task.CompletedTask);

        await middleware.InvokeAsync(context);

        var correlationId = Assert.IsType<string>(context.Items["CorrelationId"]);
        Assert.Equal(32, correlationId.Length);
        Assert.NotEqual(value, correlationId);
    }

    [Fact]
    public async Task InvokeAsync_WithOversizedCorrelationId_GeneratesServerValue()
    {
        var context = new DefaultHttpContext();
        context.Request.Headers["X-Correlation-Id"] = new string('a', 81);
        var middleware = new CorrelationIdMiddleware(_ => Task.CompletedTask);

        await middleware.InvokeAsync(context);

        var correlationId = Assert.IsType<string>(context.Items["CorrelationId"]);
        Assert.Equal(32, correlationId.Length);
    }
}
