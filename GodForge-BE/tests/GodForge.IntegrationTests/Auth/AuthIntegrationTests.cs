using System.Net;
using System.Net.Http.Json;
using GodForge.Application.Common.Models;
using GodForge.Application.Features.Auth.Commands.Login;
using GodForge.Application.Features.Auth.DTOs;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace GodForge.IntegrationTests.Auth;

public class AuthIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public AuthIntegrationTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Login_WithInvalidCredentials_ReturnsUnauthorized()
    {
        // Arrange
        var command = new LoginCommand("invalid@domain.com", "wrongpassword");

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/auth/login", command);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task SendOtp_WithValidEmail_ReturnsAcceptedOrNoContent()
    {
        // Arrange
        var command = new { Email = "test@domain.com" };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/auth/register/send-otp", command);

        // Assert
        // Standardize ApiResponse might return Ok(new { meta = ... })
        // Either way, it should be a successful status code
        Assert.True(response.IsSuccessStatusCode);
    }
}

