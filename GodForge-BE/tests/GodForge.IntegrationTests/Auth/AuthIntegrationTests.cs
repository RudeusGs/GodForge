using System.Net;
using System.Net.Http.Json;
using GodForge.Api.Services;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;
using Xunit;

namespace GodForge.IntegrationTests.Auth;

public class AuthIntegrationTests : IClassFixture<CustomWebApplicationFactory>, IDisposable
{
    private readonly HttpClient _client;
    private readonly WebApplicationFactory<Program> _isolatedFactory;

    public AuthIntegrationTests(CustomWebApplicationFactory factory)
    {
        _isolatedFactory = factory.WithWebHostBuilder(_ => { });
        _client = _isolatedFactory.CreateClient();
    }

    public void Dispose()
    {
        _client.Dispose();
        _isolatedFactory.Dispose();
        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task Login_WithInvalidCredentials_ReturnsUnauthorized()
    {
        // Arrange
        var command = new { Email = "invalid@domain.com", Password = "wrongpassword", DeviceName = "integration-test" };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/auth/login", command);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Refresh_WithoutCookie_ReturnsUnauthorized()
    {
        var response = await _client.PostAsync("/api/v1/auth/refresh", content: null);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task SendOtp_WithValidEmail_ReturnsAccepted()
    {
        // Arrange
        var command = new { Email = "test@domain.com" };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/auth/send-otp", command);

        // Assert
        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
    }

    [Fact]
    public async Task AuthEndpoints_UseIndependentIpBuckets_AndReturnRetryAfter()
    {
        var limiter = new Mock<IDistributedAuthRateLimiter>();
        limiter.Setup(value => value.ConsumeAsync(
                "send-otp",
                It.IsAny<string>(),
                It.IsAny<int>(),
                It.IsAny<TimeSpan>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(DistributedRateLimitDecision.Reject(TimeSpan.FromSeconds(17)));
        limiter.Setup(value => value.ConsumeAsync(
                "login",
                It.IsAny<string>(),
                It.IsAny<int>(),
                It.IsAny<TimeSpan>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(DistributedRateLimitDecision.Permit());
        using var factory = _isolatedFactory.WithWebHostBuilder(builder => builder.ConfigureServices(services =>
        {
            services.RemoveAll<IDistributedAuthRateLimiter>();
            services.AddSingleton(limiter.Object);
        }));
        using var client = factory.CreateClient();

        var rejected = await client.PostAsJsonAsync("/api/v1/auth/send-otp", new { Email = "bucket@domain.com" });
        Assert.Equal(HttpStatusCode.TooManyRequests, rejected.StatusCode);
        Assert.True(rejected.Headers.TryGetValues("Retry-After", out var retryAfter));
        Assert.Equal(17, int.Parse(Assert.Single(retryAfter), System.Globalization.CultureInfo.InvariantCulture));

        var login = await client.PostAsJsonAsync("/api/v1/auth/login", new
        {
            Email = "independent-login@domain.com",
            Password = "wrongpassword",
            DeviceName = "integration-test"
        });
        Assert.Equal(HttpStatusCode.Unauthorized, login.StatusCode);
    }

    [Fact]
    public async Task Login_WhenDistributedRateLimiterIsUnavailable_FailsClosed()
    {
        var limiter = new Mock<IDistributedAuthRateLimiter>();
        limiter.Setup(value => value.ConsumeAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<int>(),
                It.IsAny<TimeSpan>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(DistributedRateLimitDecision.Unavailable());
        using var factory = _isolatedFactory.WithWebHostBuilder(builder => builder.ConfigureServices(services =>
        {
            services.RemoveAll<IDistributedAuthRateLimiter>();
            services.AddSingleton(limiter.Object);
        }));
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/v1/auth/login", new
        {
            Email = "redis-failure@example.com",
            Password = "password",
            DeviceName = "integration-test"
        });

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        var payload = await response.Content.ReadAsStringAsync();
        Assert.Contains("DEPENDENCY_UNAVAILABLE", payload, StringComparison.Ordinal);
    }
}
