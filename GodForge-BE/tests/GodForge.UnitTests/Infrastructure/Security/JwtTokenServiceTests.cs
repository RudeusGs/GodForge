using System.IdentityModel.Tokens.Jwt;
using GodForge.Domain.Entities.Identity;
using GodForge.Domain.Enums;
using GodForge.Infrastructure.Configuration;
using GodForge.Infrastructure.Security;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace GodForge.UnitTests.Infrastructure.Security;

public class JwtTokenServiceTests
{
    private readonly JwtSettings _jwtSettings;
    private readonly JwtTokenService _sut;

    public JwtTokenServiceTests()
    {
        _jwtSettings = new JwtSettings
        {
            Secret = "A_VERY_LONG_SECRET_KEY_FOR_TESTING_12345",
            Issuer = "TestIssuer",
            Audience = "TestAudience",
            ExpiryMinutes = 15
        };

        var options = new Mock<IOptions<JwtSettings>>();
        options.SetupGet(x => x.Value).Returns(_jwtSettings);
        _sut = new JwtTokenService(options.Object);
    }

    [Fact]
    public void GenerateAccessToken_ReturnsJwtBoundToSession()
    {
        var now = DateTimeOffset.UtcNow;
        var sessionId = Guid.NewGuid();
        var user = User.Create("test@example.com", "Test User", "hash", now);
        user.UpdateSystemRole(SystemRole.SystemAdmin, DateTimeOffset.UtcNow);

        var result = _sut.GenerateAccessToken(user, sessionId, now);

        Assert.False(string.IsNullOrWhiteSpace(result.Token));
        Assert.Equal(now.AddMinutes(_jwtSettings.ExpiryMinutes), result.ExpiresAt);

        var token = new JwtSecurityTokenHandler().ReadJwtToken(result.Token);
        Assert.Equal(_jwtSettings.Issuer, token.Issuer);
        Assert.Equal(_jwtSettings.Audience, token.Audiences.Single());
        Assert.Equal(user.Id.ToString(), token.Claims.Single(c => c.Type == JwtRegisteredClaimNames.Sub).Value);
        Assert.Equal(user.Email, token.Claims.Single(c => c.Type == JwtRegisteredClaimNames.Email).Value);
        Assert.Equal(user.SecurityStamp, token.Claims.Single(c => c.Type == "security_stamp").Value);
        Assert.Equal(sessionId.ToString(), token.Claims.Single(c => c.Type == "sid").Value);
        Assert.False(string.IsNullOrWhiteSpace(token.Claims.Single(c => c.Type == JwtRegisteredClaimNames.Jti).Value));
    }
}
