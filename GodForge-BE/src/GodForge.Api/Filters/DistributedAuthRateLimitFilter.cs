using GodForge.Api.Contracts.Auth;
using GodForge.Api.Services;
using GodForge.Application.Common.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace GodForge.Api.Filters;

[AttributeUsage(AttributeTargets.Method)]
public sealed class DistributedAuthRateLimitAttribute : TypeFilterAttribute
{
    public DistributedAuthRateLimitAttribute(string policy) : base(typeof(DistributedAuthRateLimitFilter))
    {
        Arguments = [policy];
    }
}

public sealed class DistributedAuthRateLimitFilter : IAsyncActionFilter
{
    private static readonly IReadOnlyDictionary<string, AuthRatePolicy> Policies =
        new Dictionary<string, AuthRatePolicy>(StringComparer.Ordinal)
        {
            ["login"] = new(10, TimeSpan.FromMinutes(1), 5, TimeSpan.FromMinutes(15)),
            ["send-otp"] = new(3, TimeSpan.FromMinutes(5), 3, TimeSpan.FromMinutes(15)),
            ["forgot-password"] = new(3, TimeSpan.FromMinutes(5), 3, TimeSpan.FromMinutes(15)),
            ["register"] = new(5, TimeSpan.FromMinutes(5), 5, TimeSpan.FromMinutes(15)),
            ["reset-password"] = new(5, TimeSpan.FromMinutes(5), 5, TimeSpan.FromMinutes(15)),
            ["refresh"] = new(30, TimeSpan.FromMinutes(1), 30, TimeSpan.FromMinutes(1))
        };

    private readonly string _policy;
    private readonly IDistributedAuthRateLimiter _limiter;
    private readonly ISecretHashService _secretHash;
    private readonly RefreshTokenCookieService _refreshTokenCookie;

    public DistributedAuthRateLimitFilter(
        string policy,
        IDistributedAuthRateLimiter limiter,
        ISecretHashService secretHash,
        RefreshTokenCookieService refreshTokenCookie)
    {
        _policy = policy;
        _limiter = limiter;
        _secretHash = secretHash;
        _refreshTokenCookie = refreshTokenCookie;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        if (!Policies.TryGetValue(_policy, out var policy))
            throw new InvalidOperationException($"Unknown distributed auth rate-limit policy '{_policy}'.");

        var ip = context.HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var ipDecision = await _limiter.ConsumeAsync(
            _policy,
            $"ip:{_secretHash.Hash(ip)}",
            policy.IpLimit,
            policy.IpWindow,
            context.HttpContext.RequestAborted);
        if (!ApplyDecision(context, ipDecision))
            return;

        var scope = GetScope(context);
        var scopeDecision = await _limiter.ConsumeAsync(
            _policy,
            $"scope:{_secretHash.Hash(scope)}",
            policy.ScopeLimit,
            policy.ScopeWindow,
            context.HttpContext.RequestAborted);
        if (!ApplyDecision(context, scopeDecision))
            return;

        await next();
    }

    private string GetScope(ActionExecutingContext context)
    {
        var request = context.ActionArguments.Values.FirstOrDefault();
        return request switch
        {
            LoginRequest value => NormalizeEmail(value.Email),
            SendOtpRequest value => NormalizeEmail(value.Email),
            ForgotPasswordRequest value => NormalizeEmail(value.Email),
            RegisterRequest value => $"{NormalizeEmail(value.Email)}:{value.Otp}",
            ResetPasswordRequest value => $"{NormalizeEmail(value.Email)}:{value.Token}",
            _ when _policy == "refresh" => _refreshTokenCookie.Read(context.HttpContext.Request) ?? "missing-refresh-cookie",
            _ => "invalid-request"
        };
    }

    private static string NormalizeEmail(string? email)
        => string.IsNullOrWhiteSpace(email)
            ? "missing-email"
            : email.Trim()[..Math.Min(email.Trim().Length, 320)].ToUpperInvariant();

    private static bool ApplyDecision(ActionExecutingContext context, DistributedRateLimitDecision decision)
    {
        if (decision.Allowed)
            return true;

        if (!decision.DependencyAvailable)
        {
            context.Result = new ObjectResult(ApiErrorResponseFactory.Create(
                context.HttpContext,
                "DEPENDENCY_UNAVAILABLE",
                "Authentication abuse protection is temporarily unavailable."))
            {
                StatusCode = StatusCodes.Status503ServiceUnavailable
            };
            return false;
        }

        context.HttpContext.Response.Headers.RetryAfter = Math.Max(1, (int)Math.Ceiling(decision.RetryAfter.TotalSeconds))
            .ToString(System.Globalization.CultureInfo.InvariantCulture);
        context.Result = new ObjectResult(ApiErrorResponseFactory.Create(
            context.HttpContext,
            "RATE_LIMIT_EXCEEDED",
            "Too many requests. Please try again later."))
        {
            StatusCode = StatusCodes.Status429TooManyRequests
        };
        return false;
    }

    private sealed record AuthRatePolicy(int IpLimit, TimeSpan IpWindow, int ScopeLimit, TimeSpan ScopeWindow);
}
