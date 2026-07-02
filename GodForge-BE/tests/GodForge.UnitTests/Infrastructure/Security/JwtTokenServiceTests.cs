using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using GodForge.Domain.Entities.Identity;
using GodForge.Domain.Enums;
using GodForge.Infrastructure.Security;
using Microsoft.Extensions.Configuration;
using Moq;

namespace GodForge.UnitTests.Infrastructure.Security;

public class JwtTokenServiceTests
{
    private readonly Mock<IConfiguration> _mockConfiguration;
    private readonly JwtTokenService _sut;

    public JwtTokenServiceTests()
    {
        _mockConfiguration = new Mock<IConfiguration>();
        _sut = new JwtTokenService(_mockConfiguration.Object);
    }

    [Fact]
    public void GenerateAccessToken_Should_ReturnValidJwt_When_ConfigIsValid()
    {
        // Arrange
        _mockConfiguration.Setup(x => x["Jwt:Secret"]).Returns("A_VERY_LONG_SECRET_KEY_FOR_TESTING_12345");
        _mockConfiguration.Setup(x => x["Jwt:Issuer"]).Returns("TestIssuer");
        _mockConfiguration.Setup(x => x["Jwt:Audience"]).Returns("TestAudience");
        _mockConfiguration.Setup(x => x["Jwt:ExpiryMinutes"]).Returns("15");

        var user = User.Create("test@example.com", "Test User", "hash", SystemRole.SystemAdmin, DateTimeOffset.UtcNow);

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
    }

    [Fact]
    public void GenerateAccessToken_Should_ThrowException_When_SecretIsMissing()
    {
        // Arrange
        _mockConfiguration.Setup(x => x["Jwt:Secret"]).Returns((string?)null);
        var user = User.Create("test@example.com", "Test User", "hash", SystemRole.SystemAdmin, DateTimeOffset.UtcNow);

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => _sut.GenerateAccessToken(user));
    }
}
