using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using GodForge.Application.Features.Auth;
using GodForge.Domain.Entities.Identity;
using GodForge.Infrastructure.Configuration;
using GodForge.Infrastructure.Security;
using GodForge.IntegrationTests.Infrastructure;
using GodForge.IntegrationTests.Persistence;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace GodForge.IntegrationTests.Auth;

[Collection(PostgresPersistenceCollection.CollectionName)]
public sealed class AuthPostgresHttpTests
{
    private readonly PostgresPersistenceFixture _fixture;

    public AuthPostgresHttpTests(PostgresPersistenceFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task MultiDevice_LoginRefreshLogoutAndRemoteRevoke_AreSessionScoped()
    {
        var credentials = await SeedUserAsync();
        using var factory = new AuthPostgresWebApplicationFactory(_fixture.ConnectionString);
        using var clientA = factory.CreateClient(SecureCookieClientOptions());
        using var clientB = factory.CreateClient(SecureCookieClientOptions());

        var loginA = await LoginAsync(clientA, credentials.Email, credentials.Password, "Chrome on Windows");
        var loginB = await LoginAsync(clientB, credentials.Email, credentials.Password, "Firefox on Linux");
        Assert.NotEqual(loginA.SessionId, loginB.SessionId);
        var initialA = await GetMeAsync(clientA, loginA.AccessToken);
        var initialB = await GetMeAsync(clientB, loginB.AccessToken);
        Assert.True(initialA.StatusCode == HttpStatusCode.OK, await initialA.Content.ReadAsStringAsync());
        Assert.True(initialB.StatusCode == HttpStatusCode.OK, await initialB.Content.ReadAsStringAsync());

        var refreshedA = await RefreshAsync(clientA);
        var refreshedB = await RefreshAsync(clientB);
        Assert.Equal(loginA.SessionId, refreshedA.SessionId);
        Assert.Equal(loginB.SessionId, refreshedB.SessionId);
        Assert.Equal(HttpStatusCode.OK, (await GetMeAsync(clientA, refreshedA.AccessToken)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await GetMeAsync(clientB, refreshedB.AccessToken)).StatusCode);

        using (var logout = new HttpRequestMessage(HttpMethod.Post, "/api/v1/auth/logout"))
        {
            logout.Headers.Authorization = new AuthenticationHeaderValue("Bearer", refreshedA.AccessToken);
            var logoutResponse = await clientA.SendAsync(logout);
            Assert.Equal(HttpStatusCode.NoContent, logoutResponse.StatusCode);
            Assert.Contains(logoutResponse.Headers.GetValues("Set-Cookie"), value =>
                value.Contains("godforge_refresh=", StringComparison.Ordinal) &&
                value.Contains("expires=", StringComparison.OrdinalIgnoreCase));
        }

        Assert.Equal(HttpStatusCode.Unauthorized, (await GetMeAsync(clientA, refreshedA.AccessToken)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await clientA.PostAsync("/api/v1/auth/refresh", null)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await GetMeAsync(clientB, refreshedB.AccessToken)).StatusCode);

        var reloginA = await LoginAsync(clientA, credentials.Email, credentials.Password, "Chrome on Windows");
        using (var revoke = new HttpRequestMessage(HttpMethod.Delete, $"/api/v1/users/me/sessions/{loginB.SessionId}"))
        {
            revoke.Headers.Authorization = new AuthenticationHeaderValue("Bearer", reloginA.AccessToken);
            Assert.Equal(HttpStatusCode.NoContent, (await clientA.SendAsync(revoke)).StatusCode);
        }

        Assert.Equal(HttpStatusCode.Unauthorized, (await GetMeAsync(clientB, refreshedB.AccessToken)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await clientB.PostAsync("/api/v1/auth/refresh", null)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await GetMeAsync(clientA, reloginA.AccessToken)).StatusCode);
    }

    [Fact]
    public async Task SameRefreshCookie_TwoConcurrentRequests_AtMostOneRotatesAndReplayRevokesFamily()
    {
        var credentials = await SeedUserAsync();
        using var factory = new AuthPostgresWebApplicationFactory(_fixture.ConnectionString);
        using var loginClient = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false });
        var loginResponse = await loginClient.PostAsJsonAsync("/api/v1/auth/login", new
        {
            credentials.Email,
            credentials.Password,
            DeviceName = "Replay test"
        });
        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);
        var auth = await ReadAuthAsync(loginResponse);
        var rawCookie = Assert.Single(loginResponse.Headers.GetValues("Set-Cookie"))
            .Split(';', 2, StringSplitOptions.TrimEntries)[0];

        using var firstClient = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false });
        using var secondClient = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false });
        using var firstRequest = new HttpRequestMessage(HttpMethod.Post, "/api/v1/auth/refresh");
        using var secondRequest = new HttpRequestMessage(HttpMethod.Post, "/api/v1/auth/refresh");
        firstRequest.Headers.Add("Cookie", rawCookie);
        secondRequest.Headers.Add("Cookie", rawCookie);

        var responses = await Task.WhenAll(firstClient.SendAsync(firstRequest), secondClient.SendAsync(secondRequest));

        Assert.Single(responses, response => response.StatusCode == HttpStatusCode.OK);
        Assert.Single(responses, response => response.StatusCode == HttpStatusCode.Unauthorized);
        await using var verificationContext = _fixture.CreateContext();
        var session = await verificationContext.UserSessions.SingleAsync(value => value.Id == auth.SessionId);
        var familyTokens = await verificationContext.RefreshTokens
            .Where(value => value.SessionId == auth.SessionId)
            .ToListAsync();
        Assert.NotNull(session.RevokedAt);
        Assert.Equal(2, familyTokens.Count);
        Assert.All(familyTokens, token => Assert.NotNull(token.RevokedAt));
    }

    [Fact]
    public async Task PasswordReset_RevokesAllDevicesAndOldCredentialsAndCannotBeReused()
    {
        var credentials = await SeedUserAsync();
        using var factory = new AuthPostgresWebApplicationFactory(_fixture.ConnectionString);
        using var clientA = factory.CreateClient(SecureCookieClientOptions());
        using var clientB = factory.CreateClient(SecureCookieClientOptions());
        var loginA = await LoginAsync(clientA, credentials.Email, credentials.Password, "Chrome");
        var loginB = await LoginAsync(clientB, credentials.Email, credentials.Password, "Firefox");
        const string resetToken = "reset-token-with-sufficient-entropy-for-integration";
        const string newPassword = "New-Correct-Horse-2026";
        await SeedResetChallengeAsync(credentials.Email, resetToken);

        var resetResponse = await clientA.PostAsJsonAsync("/api/v1/auth/reset-password", new
        {
            credentials.Email,
            Token = resetToken,
            NewPassword = newPassword
        });

        Assert.Equal(HttpStatusCode.NoContent, resetResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await GetMeAsync(clientA, loginA.AccessToken)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await GetMeAsync(clientB, loginB.AccessToken)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await clientA.PostAsync("/api/v1/auth/refresh", null)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await clientB.PostAsync("/api/v1/auth/refresh", null)).StatusCode);

        var reused = await clientA.PostAsJsonAsync("/api/v1/auth/reset-password", new
        {
            credentials.Email,
            Token = resetToken,
            NewPassword = "Another-Correct-Horse-2026"
        });
        Assert.Equal(HttpStatusCode.BadRequest, reused.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await clientA.PostAsJsonAsync("/api/v1/auth/login", new
        {
            credentials.Email,
            credentials.Password,
            DeviceName = "old password"
        })).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await clientA.PostAsJsonAsync("/api/v1/auth/login", new
        {
            credentials.Email,
            Password = newPassword,
            DeviceName = "new password"
        })).StatusCode);
    }

    [Fact]
    public async Task PasswordReset_SameChallengeConsumedConcurrently_OnlyOneSucceeds()
    {
        var credentials = await SeedUserAsync();
        const string resetToken = "concurrent-reset-token-with-sufficient-entropy";
        await SeedResetChallengeAsync(credentials.Email, resetToken);
        using var factory = new AuthPostgresWebApplicationFactory(_fixture.ConnectionString);
        using var first = factory.CreateClient();
        using var second = factory.CreateClient();
        var payload = new { credentials.Email, Token = resetToken, NewPassword = "Concurrent-New-Password-2026" };

        var responses = await Task.WhenAll(
            first.PostAsJsonAsync("/api/v1/auth/reset-password", payload),
            second.PostAsJsonAsync("/api/v1/auth/reset-password", payload));

        Assert.Single(responses, response => response.StatusCode == HttpStatusCode.NoContent);
        Assert.Single(responses, response => response.StatusCode == HttpStatusCode.BadRequest);
        await using var context = _fixture.CreateContext();
        var challenge = await context.AuthChallenges.SingleAsync(value =>
            value.NormalizedEmail == User.NormalizeEmail(credentials.Email) && value.Purpose == AuthChallengePurposes.PasswordReset);
        Assert.NotNull(challenge.ConsumedAt);
    }

    [Fact]
    public async Task PasswordReset_RacingRefresh_LeavesNoActiveSessionOrTokenDescendant()
    {
        var credentials = await SeedUserAsync();
        const string resetToken = "reset-versus-refresh-token-with-sufficient-entropy";
        await SeedResetChallengeAsync(credentials.Email, resetToken);
        using var factory = new AuthPostgresWebApplicationFactory(_fixture.ConnectionString);
        using var sessionClient = factory.CreateClient(SecureCookieClientOptions());
        using var resetClient = factory.CreateClient();
        var login = await LoginAsync(sessionClient, credentials.Email, credentials.Password, "Reset race");

        var resetTask = resetClient.PostAsJsonAsync("/api/v1/auth/reset-password", new
        {
            credentials.Email,
            Token = resetToken,
            NewPassword = "Reset-Race-New-Password-2026"
        });
        var refreshTask = sessionClient.PostAsync("/api/v1/auth/refresh", null);
        await Task.WhenAll(resetTask, refreshTask);

        Assert.Equal(HttpStatusCode.NoContent, (await resetTask).StatusCode);
        Assert.Contains((await refreshTask).StatusCode, new[] { HttpStatusCode.OK, HttpStatusCode.Unauthorized });
        await using var context = _fixture.CreateContext();
        var session = await context.UserSessions.SingleAsync(value => value.Id == login.SessionId);
        var tokens = await context.RefreshTokens.Where(value => value.SessionId == login.SessionId).ToListAsync();
        Assert.NotNull(session.RevokedAt);
        Assert.NotEmpty(tokens);
        Assert.All(tokens, token => Assert.NotNull(token.RevokedAt));
        Assert.Equal(HttpStatusCode.Unauthorized, (await sessionClient.PostAsync("/api/v1/auth/refresh", null)).StatusCode);
    }

    [Fact]
    public async Task RemoteRevoke_RacingRefresh_LeavesOnlySelectedSessionRevoked()
    {
        var credentials = await SeedUserAsync();
        using var factory = new AuthPostgresWebApplicationFactory(_fixture.ConnectionString);
        using var clientA = factory.CreateClient(SecureCookieClientOptions());
        using var clientB = factory.CreateClient(SecureCookieClientOptions());
        var loginA = await LoginAsync(clientA, credentials.Email, credentials.Password, "Revoke authority");
        var loginB = await LoginAsync(clientB, credentials.Email, credentials.Password, "Revoke target");
        using var revokeRequest = new HttpRequestMessage(HttpMethod.Delete, $"/api/v1/users/me/sessions/{loginB.SessionId}");
        revokeRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", loginA.AccessToken);

        var revokeTask = clientA.SendAsync(revokeRequest);
        var refreshTask = clientB.PostAsync("/api/v1/auth/refresh", null);
        await Task.WhenAll(revokeTask, refreshTask);

        Assert.Equal(HttpStatusCode.NoContent, (await revokeTask).StatusCode);
        Assert.Contains((await refreshTask).StatusCode, new[] { HttpStatusCode.OK, HttpStatusCode.Unauthorized });
        Assert.Equal(HttpStatusCode.Unauthorized, (await clientB.PostAsync("/api/v1/auth/refresh", null)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await GetMeAsync(clientA, loginA.AccessToken)).StatusCode);
        await using var context = _fixture.CreateContext();
        var target = await context.UserSessions.SingleAsync(value => value.Id == loginB.SessionId);
        var authority = await context.UserSessions.SingleAsync(value => value.Id == loginA.SessionId);
        Assert.NotNull(target.RevokedAt);
        Assert.Null(authority.RevokedAt);
    }

    [Fact]
    public async Task RegistrationOtp_SameChallengeConsumedConcurrently_CreatesExactlyOneUser()
    {
        var suffix = Guid.NewGuid().ToString("N");
        var email = $"concurrent-register-{suffix}@example.com";
        const string otp = "123456";
        await SeedRegistrationChallengeAsync(email, otp);
        using var factory = new AuthPostgresWebApplicationFactory(_fixture.ConnectionString);
        using var first = factory.CreateClient();
        using var second = factory.CreateClient();
        var payload = new { Email = email, Otp = otp, Password = "Concurrent-Register-2026", DisplayName = "Concurrent User" };

        var responses = await Task.WhenAll(
            first.PostAsJsonAsync("/api/v1/auth/register", payload),
            second.PostAsJsonAsync("/api/v1/auth/register", payload));

        Assert.Single(responses, response => response.StatusCode == HttpStatusCode.Created);
        Assert.Single(responses, response => response.StatusCode is HttpStatusCode.BadRequest or HttpStatusCode.Conflict);
        await using var context = _fixture.CreateContext();
        Assert.Equal(1, await context.Users.CountAsync(value => value.NormalizedEmail == User.NormalizeEmail(email)));
        var challenge = await context.AuthChallenges.SingleAsync(value =>
            value.NormalizedEmail == User.NormalizeEmail(email) && value.Purpose == AuthChallengePurposes.Registration);
        Assert.NotNull(challenge.ConsumedAt);
    }

    [Fact]
    public async Task Login_FiveConcurrentFailures_SerializesCounterAndLocksAccount()
    {
        var credentials = await SeedUserAsync();
        using var factory = new AuthPostgresWebApplicationFactory(_fixture.ConnectionString);
        using var client = factory.CreateClient();

        var responses = await Task.WhenAll(Enumerable.Range(0, 5).Select(_ =>
            client.PostAsJsonAsync("/api/v1/auth/login", new
            {
                credentials.Email,
                Password = "definitely-wrong-password",
                DeviceName = "Concurrent lockout test"
            })));

        Assert.All(responses, response => Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode));
        await using var context = _fixture.CreateContext();
        var user = await context.Users.SingleAsync(value => value.NormalizedEmail == User.NormalizeEmail(credentials.Email));
        Assert.Equal(5, user.FailedLoginCount);
        Assert.Equal(GodForge.Domain.Enums.UserStatus.Locked, user.Status);
        Assert.True(user.LockedUntil > DateTimeOffset.UtcNow);
    }

    private async Task<Credentials> SeedUserAsync()
    {
        var suffix = Guid.NewGuid().ToString("N");
        var email = $"auth-http-{suffix}@example.com";
        const string password = "Correct-Horse-2026";
        await using var context = _fixture.CreateContext();
        var user = User.Create(email, "Auth HTTP User", new PasswordHasher().HashPassword(password), DateTimeOffset.UtcNow);
        user.MarkEmailVerified(DateTimeOffset.UtcNow);
        context.Users.Add(user);
        await context.SaveChangesAsync();
        return new Credentials(email, password);
    }

    private async Task SeedResetChallengeAsync(string email, string rawToken)
    {
        var hashService = new SecretHashService(Options.Create(new SecretHashSettings
        {
            Key = "auth-http-test-secret-hash-key-64-characters-minimum-000000000"
        }));
        await using var context = _fixture.CreateContext();
        var user = await context.Users.SingleAsync(value => value.NormalizedEmail == User.NormalizeEmail(email));
        var now = DateTimeOffset.UtcNow;
        context.AuthChallenges.Add(AuthChallenge.Create(
            user.NormalizedEmail,
            AuthChallengePurposes.PasswordReset,
            hashService.Hash(rawToken),
            now,
            TimeSpan.FromHours(1),
            TimeSpan.FromMinutes(1)));
        await context.SaveChangesAsync();
    }

    private async Task SeedRegistrationChallengeAsync(string email, string otp)
    {
        var hashService = new SecretHashService(Options.Create(new SecretHashSettings
        {
            Key = "auth-http-test-secret-hash-key-64-characters-minimum-000000000"
        }));
        await using var context = _fixture.CreateContext();
        var now = DateTimeOffset.UtcNow;
        context.AuthChallenges.Add(AuthChallenge.Create(
            User.NormalizeEmail(email),
            AuthChallengePurposes.Registration,
            hashService.Hash(otp),
            now,
            TimeSpan.FromMinutes(5),
            TimeSpan.FromMinutes(1)));
        await context.SaveChangesAsync();
    }

    private static async Task<AuthState> LoginAsync(HttpClient client, string email, string password, string deviceName)
    {
        var response = await client.PostAsJsonAsync("/api/v1/auth/login", new { Email = email, Password = password, DeviceName = deviceName });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return await ReadAuthAsync(response);
    }

    private static WebApplicationFactoryClientOptions SecureCookieClientOptions() => new()
    {
        HandleCookies = true,
        BaseAddress = new Uri("https://localhost")
    };

    private static async Task<AuthState> RefreshAsync(HttpClient client)
    {
        var response = await client.PostAsync("/api/v1/auth/refresh", null);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return await ReadAuthAsync(response);
    }

    private static Task<HttpResponseMessage> GetMeAsync(HttpClient client, string accessToken)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/users/me");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        return client.SendAsync(request);
    }

    private static async Task<AuthState> ReadAuthAsync(HttpResponseMessage response)
    {
        var document = await response.Content.ReadFromJsonAsync<JsonElement>();
        var data = document.GetProperty("data");
        return new AuthState(
            data.GetProperty("accessToken").GetString()!,
            data.GetProperty("session").GetProperty("id").GetGuid());
    }

    private sealed record Credentials(string Email, string Password);
    private sealed record AuthState(string AccessToken, Guid SessionId);
}
