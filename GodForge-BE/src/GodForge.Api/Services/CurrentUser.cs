using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using GodForge.Application.Common.Interfaces;

namespace GodForge.Api.Services;

public sealed class CurrentUser : ICurrentUser
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUser(IHttpContextAccessor httpContextAccessor) => _httpContextAccessor = httpContextAccessor;

    public Guid? Id => ReadGuidClaim(ClaimTypes.NameIdentifier, JwtRegisteredClaimNames.Sub);
    public Guid? SessionId => ReadGuidClaim("sid");
    public bool IsAuthenticated => _httpContextAccessor.HttpContext?.User?.Identity?.IsAuthenticated ?? false;
    public string? Email => ReadClaim(ClaimTypes.Email, JwtRegisteredClaimNames.Email);
    public string? SystemRole => ReadClaim("role");
    public string? Jti => ReadClaim(JwtRegisteredClaimNames.Jti);

    public DateTimeOffset? TokenExpiration
    {
        get
        {
            var value = ReadClaim(JwtRegisteredClaimNames.Exp);
            return long.TryParse(value, out var seconds) ? DateTimeOffset.FromUnixTimeSeconds(seconds) : null;
        }
    }

    public Guid GetId() => Id ?? throw new UnauthorizedAccessException("User is not authenticated.");
    public Guid GetSessionId() => SessionId ?? throw new UnauthorizedAccessException("The access token has no active session.");

    private string? ReadClaim(params string[] claimTypes)
    {
        var principal = _httpContextAccessor.HttpContext?.User;
        foreach (var claimType in claimTypes)
        {
            var value = principal?.FindFirst(claimType)?.Value;
            if (!string.IsNullOrWhiteSpace(value))
                return value;
        }
        return null;
    }

    private Guid? ReadGuidClaim(params string[] claimTypes)
        => Guid.TryParse(ReadClaim(claimTypes), out var value) ? value : null;
}
