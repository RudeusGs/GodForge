using GodForge.Api.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;

namespace GodForge.UnitTests.Api.Services;

public sealed class RefreshTokenCookieServiceTests
{
    [Fact]
    public void Write_InProduction_CreatesHttpOnlySecureStrictCookie()
    {
        var service = new RefreshTokenCookieService(new TestHostEnvironment(Environments.Production));
        var context = new DefaultHttpContext();

        service.Write(context.Response, "refresh-secret", DateTimeOffset.UtcNow.AddDays(30));

        var header = Assert.Single(context.Response.Headers["Set-Cookie"])!.ToString();
        Assert.Contains("godforge_refresh=refresh-secret", header, StringComparison.Ordinal);
        Assert.Contains("httponly", header, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("secure", header, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("samesite=strict", header, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("path=/api/v1/auth", header, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Read_ReturnsCookieValue()
    {
        var service = new RefreshTokenCookieService(new TestHostEnvironment(Environments.Development));
        var context = new DefaultHttpContext();
        context.Request.Headers.Cookie = "godforge_refresh=refresh-secret";

        var token = service.Read(context.Request);

        Assert.Equal("refresh-secret", token);
    }

    private sealed class TestHostEnvironment(string environmentName) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = environmentName;
        public string ApplicationName { get; set; } = "GodForge.UnitTests";
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
