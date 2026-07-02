using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using GodForge.Api.Services;
using Microsoft.AspNetCore.Http;
using Moq;

namespace GodForge.UnitTests.Api.Services;

public class CurrentUserTests
{
    private readonly Mock<IHttpContextAccessor> _mockHttpContextAccessor;
    private readonly CurrentUser _sut;

    public CurrentUserTests()
    {
        _mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        _sut = new CurrentUser(_mockHttpContextAccessor.Object);
    }

    [Fact]
    public void Id_Should_ReturnGuid_When_SubClaimExists()
    {
        // Arrange
        var id = Guid.NewGuid();
        var claims = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.Sub, id.ToString())
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var claimsPrincipal = new ClaimsPrincipal(identity);
        var httpContext = new DefaultHttpContext { User = claimsPrincipal };
        _mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(httpContext);

        // Act
        var resultId = _sut.Id;

        // Assert
        Assert.Equal(id, resultId);
    }

    [Fact]
    public void Id_Should_ReturnGuid_When_NameIdentifierClaimExists()
    {
        // Arrange
        var id = Guid.NewGuid();
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, id.ToString())
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var claimsPrincipal = new ClaimsPrincipal(identity);
        var httpContext = new DefaultHttpContext { User = claimsPrincipal };
        _mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(httpContext);

        // Act
        var resultId = _sut.Id;

        // Assert
        Assert.Equal(id, resultId);
    }

    [Fact]
    public void Email_Should_ReturnEmail_When_EmailClaimExists()
    {
        // Arrange
        var email = "test@example.com";
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Email, email)
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var claimsPrincipal = new ClaimsPrincipal(identity);
        var httpContext = new DefaultHttpContext { User = claimsPrincipal };
        _mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(httpContext);

        // Act
        var resultEmail = _sut.Email;

        // Assert
        Assert.Equal(email, resultEmail);
    }

    [Fact]
    public void GetId_Should_ThrowUnauthorizedAccessException_When_UserIsNotAuthenticated()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        _mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(httpContext);

        // Act & Assert
        Assert.Throws<UnauthorizedAccessException>(() => _sut.GetId());
    }
}
