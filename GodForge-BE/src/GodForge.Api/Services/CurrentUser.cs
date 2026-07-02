using System.Security.Claims;
using GodForge.Application.Common.Interfaces;
using Microsoft.AspNetCore.Http;

namespace GodForge.Api.Services;

public sealed class CurrentUser : ICurrentUser
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUser(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Guid? Id
    {
        get
        {
            var idClaim = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)
                       ?? _httpContextAccessor.HttpContext?.User?.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub);
            if (idClaim != null && Guid.TryParse(idClaim.Value, out var id))
            {
                return id;
            }
            return null;
        }
    }

    public bool IsAuthenticated => _httpContextAccessor.HttpContext?.User?.Identity?.IsAuthenticated ?? false;

    public string? Email => _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.Email)?.Value
                         ?? _httpContextAccessor.HttpContext?.User?.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Email)?.Value;

    public string? SystemRole => _httpContextAccessor.HttpContext?.User?.FindFirst("role")?.Value;

    public Guid GetId() => Id ?? throw new UnauthorizedAccessException("User is not authenticated.");
}
