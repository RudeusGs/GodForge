using GodForge.Application.Common.Interfaces;

namespace GodForge.Api.Services;

public sealed class HttpRequestContext : IRequestContext
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public HttpRequestContext(IHttpContextAccessor httpContextAccessor)
        => _httpContextAccessor = httpContextAccessor;

    public string CorrelationId
    {
        get
        {
            var context = _httpContextAccessor.HttpContext;
            return context?.Items["CorrelationId"]?.ToString()
                   ?? context?.TraceIdentifier
                   ?? Guid.NewGuid().ToString("N");
        }
    }

    public string? IpAddress
        => _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString();

    public string? UserAgent
        => _httpContextAccessor.HttpContext?.Request.Headers.UserAgent.ToString();
}
