using GodForge.Domain.Entities.Identity;

namespace GodForge.Application.Common.Interfaces;

public interface ITokenService
{
    string GenerateAccessToken(User user);
    string GenerateRefreshToken();
    string HashRefreshToken(string token);
}
