using System.Net;
using GodForge.IntegrationTests.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace GodForge.IntegrationTests.Api;

[Collection(PostgresPersistenceCollection.CollectionName)]
public class CorsIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly PostgresPersistenceFixture _postgres;

    public CorsIntegrationTests(CustomWebApplicationFactory factory, PostgresPersistenceFixture postgres)
    {
        _factory = factory;
        _postgres = postgres;
    }

    [Fact]
    public async Task DevelopmentMode_AllowsLocalhost5174Origin()
    {
        using var devFactory = _factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Development");
            // Development startup applies migrations. Use only the isolated fixture,
            // never a developer's local .env/default database configuration.
            builder.ConfigureAppConfiguration((_, configuration) =>
                configuration.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:DefaultConnection"] = _postgres.ConnectionString
                }));
        });
        using var client = devFactory.CreateClient();

        using var request = new HttpRequestMessage(HttpMethod.Options, "/api/v1/auth/refresh");
        request.Headers.Add("Origin", "http://localhost:5174");
        request.Headers.Add("Access-Control-Request-Method", "POST");
        request.Headers.Add("Access-Control-Request-Headers", "authorization,content-type");

        var response = await client.SendAsync(request);

        Assert.True(response.Headers.Contains("Access-Control-Allow-Origin"));
        Assert.Equal("http://localhost:5174", response.Headers.GetValues("Access-Control-Allow-Origin").First());
        Assert.Equal("true", response.Headers.GetValues("Access-Control-Allow-Credentials").First());
    }

    [Fact]
    public async Task TestingMode_AllowsConfiguredBaseUrlOrigin()
    {
        using var client = _factory.CreateClient();

        using var request = new HttpRequestMessage(HttpMethod.Options, "/api/v1/auth/refresh");
        request.Headers.Add("Origin", "https://frontend.integration.test");
        request.Headers.Add("Access-Control-Request-Method", "POST");
        request.Headers.Add("Access-Control-Request-Headers", "authorization,content-type");

        var response = await client.SendAsync(request);

        Assert.True(response.Headers.Contains("Access-Control-Allow-Origin"));
        Assert.Equal("https://frontend.integration.test", response.Headers.GetValues("Access-Control-Allow-Origin").First());
    }

    [Fact]
    public async Task TestingMode_RejectsUntrustedOrigin()
    {
        using var client = _factory.CreateClient();

        using var request = new HttpRequestMessage(HttpMethod.Options, "/api/v1/auth/refresh");
        request.Headers.Add("Origin", "https://untrusted-domain.com");
        request.Headers.Add("Access-Control-Request-Method", "POST");

        var response = await client.SendAsync(request);

        Assert.False(response.Headers.Contains("Access-Control-Allow-Origin"));
    }
}
