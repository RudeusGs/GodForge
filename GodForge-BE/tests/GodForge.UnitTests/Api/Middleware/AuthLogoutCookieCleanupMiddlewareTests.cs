using GodForge.Api.Middleware;
using GodForge.Api.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;

namespace GodForge.UnitTests.Api.Middleware;

public sealed class AuthLogoutCookieCleanupMiddlewareTests
{
    [Theory]
    [InlineData(204)]
    [InlineData(500)]
    public async Task InvokeAsync_LogoutResponse_AlwaysDeletesRefreshCookie(int statusCode)
    {
        var context = new DefaultHttpContext();
        context.Request.Method = HttpMethods.Post;
        context.Request.Path = "/api/v1/auth/logout";
        var middleware = new AuthLogoutCookieCleanupMiddleware(nextContext =>
        {
            nextContext.Response.StatusCode = statusCode;
            return Task.CompletedTask;
        });

        await middleware.InvokeAsync(context, CreateCookieService());

        AssertDeleteCookie(context);
    }

    [Fact]
    public async Task InvokeAsync_HandlerThrows_DeletesCookieAndPreservesFailure()
    {
        var context = new DefaultHttpContext();
        context.Request.Method = HttpMethods.Post;
        context.Request.Path = "/api/v1/auth/logout";
        var middleware = new AuthLogoutCookieCleanupMiddleware(_ => throw new InvalidOperationException("database failed"));

        await Assert.ThrowsAsync<InvalidOperationException>(() => middleware.InvokeAsync(context, CreateCookieService()));

        AssertDeleteCookie(context);
    }

    [Fact]
    public async Task InvokeAsync_NonLogout_DoesNotChangeCookies()
    {
        var context = new DefaultHttpContext();
        context.Request.Method = HttpMethods.Post;
        context.Request.Path = "/api/v1/auth/refresh";
        var middleware = new AuthLogoutCookieCleanupMiddleware(_ => Task.CompletedTask);

        await middleware.InvokeAsync(context, CreateCookieService());

        Assert.Equal(0, context.Response.Headers.SetCookie.Count);
    }

    private static RefreshTokenCookieService CreateCookieService()
        => new(new TestHostEnvironment(Environments.Production));

    private static void AssertDeleteCookie(DefaultHttpContext context)
    {
        var header = Assert.Single(context.Response.Headers.SetCookie)!;
        Assert.Contains("godforge_refresh=", header, StringComparison.Ordinal);
        Assert.Contains("expires=", header, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("httponly", header, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("secure", header, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("samesite=strict", header, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("path=/api/v1/auth", header, StringComparison.OrdinalIgnoreCase);
    }

    private sealed class TestHostEnvironment(string environmentName) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = environmentName;
        public string ApplicationName { get; set; } = "GodForge.UnitTests";
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
