using Microsoft.Extensions.DependencyInjection;
using Swashbuckle.AspNetCore.Swagger;

namespace GodForge.IntegrationTests.Auth;

public sealed class IdentityOpenApiTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public IdentityOpenApiTests(CustomWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public void OpenApiGeneration_IncludesEveryM1IdentityRoute()
    {
        using var scope = _factory.Services.CreateScope();
        var document = scope.ServiceProvider.GetRequiredService<ISwaggerProvider>().GetSwagger("v1");
        var expectedPaths = new[]
        {
            "/api/v1/auth/send-otp",
            "/api/v1/auth/register",
            "/api/v1/auth/login",
            "/api/v1/auth/refresh",
            "/api/v1/auth/logout",
            "/api/v1/auth/forgot-password",
            "/api/v1/auth/reset-password",
            "/api/v1/users/me",
            "/api/v1/users/me/sessions",
            "/api/v1/users/me/sessions/{sessionId}"
        };

        foreach (var path in expectedPaths)
            Assert.True(document.Paths.ContainsKey(path), $"OpenAPI is missing Identity route '{path}'.");
    }
}
