using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using GodForge.Domain.Entities.Identity;
using GodForge.Domain.Enums;
using GodForge.Infrastructure.Configuration;
using GodForge.Infrastructure.Security;
using Microsoft.Extensions.Options;
using Moq;

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

        var optionsMock = new Mock<IOptions<JwtSettings>>();
        optionsMock.Setup(o => o.Value).Returns(_jwtSettings);
        _sut = new JwtTokenService(optionsMock.Object);
    }

    [Fact]
    public void GenerateAccessToken_Should_ReturnValidJwt_When_ConfigIsValid()
    {
        // Arrange
        var user = User.Create("test@example.com", "Test User", "hash", DateTimeOffset.UtcNow);
        user.UpdateSystemRole(SystemRole.SystemAdmin);

        // Act
        var tokenString = _sut.GenerateAccessToken(user);

        // Assert
        Assert.False(string.IsNullOrWhiteSpace(tokenString));

        var handler = new JwtSecurityTokenHandler();
        var token = handler.ReadJwtToken(tokenString);

        Assert.Equal("TestIssuer", token.Issuer);
        Assert.Equal("TestAudience", token.Audiences.First());

        var subClaim = token.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Sub);
        Assert.NotNull(subClaim);
        Assert.Equal(user.Id.ToString(), subClaim.Value);

        var emailClaim = token.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Email);
        Assert.NotNull(emailClaim);
        Assert.Equal(user.Email, emailClaim.Value);

        var securityStampClaim = token.Claims.FirstOrDefault(c => c.Type == "security_stamp");
        Assert.NotNull(securityStampClaim);
        Assert.Equal(user.SecurityStamp, securityStampClaim.Value);
    }
}


