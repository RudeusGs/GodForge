using GodForge.Api.Services;

namespace GodForge.Api.Middleware;

public sealed class AuthLogoutCookieCleanupMiddleware
{
    private const string LogoutPath = "/api/v1/auth/logout";
    private readonly RequestDelegate _next;

    public AuthLogoutCookieCleanupMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context, RefreshTokenCookieService refreshTokenCookie)
    {
        var isLogout = HttpMethods.IsPost(context.Request.Method) &&
                       context.Request.Path.Equals(LogoutPath, StringComparison.OrdinalIgnoreCase);

        try
        {
            await _next(context);
        }
        finally
        {
            // Clearing the browser credential is independent from the durable revocation result.
            // This middleware wraps exception handling, so its header is added after a sanitized
            // failure response has been constructed and cannot be erased by Response.Clear().
            if (isLogout && !context.Response.HasStarted)
                refreshTokenCookie.Delete(context.Response);
        }
    }
}
