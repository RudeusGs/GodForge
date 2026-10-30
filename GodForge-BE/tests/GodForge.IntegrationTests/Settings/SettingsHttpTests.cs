using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using GodForge.Application.Common.Interfaces;
using GodForge.Application.Features.Settings;
using GodForge.Domain.Entities.Core;
using GodForge.Domain.Entities.Identity;
using GodForge.Domain.Enums;
using GodForge.Infrastructure.Persistence.Repositories;
using GodForge.IntegrationTests.Infrastructure;
using GodForge.IntegrationTests.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace GodForge.IntegrationTests.Settings;

[Collection(PostgresPersistenceCollection.CollectionName)]
public sealed class SettingsHttpTests(PostgresPersistenceFixture fixture)
{
    private static readonly DateTimeOffset Now = DateTimeOffset.UtcNow;
    private static object Preferences(long version = 1) => new { theme = "dark", notificationInApp = false, notificationEmail = false, notificationDigest = "weekly", version };
    private static object ProjectInput(bool ai, long version = 1, string profile = "current-default-v1") => new
    { analysisProfileKey = profile, aiAdvisoryEnabled = ai, defaultAssetVisibility = "internal", notificationPolicyVersion = 1, version };

    [Fact]
    public async Task UserPreferences_InitializesOnceAndConcurrentUpdatesConflictWithAtomicAudit()
    {
        var s = await SeedAsync();
        using var factory = new PostgresWebApplicationFactory(fixture.ConnectionString);
        using var client = Client(factory, s.Owner);
        var initial = await Task.WhenAll(client.GetAsync("/api/v1/users/me/settings"), client.GetAsync("/api/v1/users/me/settings"));
        foreach (var response in initial)
        {
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var data = await Data(response);
            Assert.Equal("light", data.GetProperty("theme").GetString());
            Assert.Equal(1, data.GetProperty("version").GetInt64());
        }
        var updates = await Task.WhenAll(client.PutAsJsonAsync("/api/v1/users/me/settings", Preferences()), client.PutAsJsonAsync("/api/v1/users/me/settings", Preferences()));
        Assert.Single(updates, r => r.StatusCode == HttpStatusCode.OK);
        Assert.Single(updates, r => r.StatusCode == HttpStatusCode.Conflict);
        var read = await Data(await client.GetAsync("/api/v1/users/me/settings"));
        Assert.Equal("dark", read.GetProperty("theme").GetString());
        Assert.Equal("weekly", read.GetProperty("notificationDigest").GetString());
        Assert.False(read.GetProperty("notificationEmail").GetBoolean());
        Assert.Equal(2, read.GetProperty("version").GetInt64());
        await using var db = fixture.CreateContext();
        var row = Assert.Single(await db.UserSettings.Where(x => x.UserId == s.Owner).ToListAsync());
        Assert.Equal(2, row.Version);
        Assert.Equal(1, await db.AuditLogs.CountAsync(a => a.ActorUserId == s.Owner && a.EventType == "user.settings_updated"));
        Assert.False(await db.UserSettings.AnyAsync(x => x.UserId == s.Member));
    }

    [Theory]
    [InlineData("{\"theme\":\"system\",\"notificationInApp\":true,\"notificationEmail\":true,\"notificationDigest\":\"off\",\"version\":1}")]
    [InlineData("{\"theme\":\"light\",\"notificationInApp\":true,\"notificationEmail\":true,\"notificationDigest\":\"hourly\",\"version\":1}")]
    [InlineData("{\"theme\":1,\"notificationInApp\":true,\"notificationEmail\":true,\"notificationDigest\":\"off\",\"version\":1}")]
    [InlineData("{\"theme\":\"dark\"}")]
    [InlineData("{\"theme\":\"dark\",\"notificationInApp\":true,\"notificationEmail\":true,\"notificationDigest\":\"off\",\"version\":1,\"userId\":\"00000000-0000-0000-0000-000000000001\"}")]
    [InlineData("{\"theme\":\"dark\",\"notificationInApp\":true,\"notificationEmail\":true,\"notificationDigest\":\"off\",\"version\":0}")]
    public async Task UserPreferences_RejectsInvalidMissingNumericAndTargetProperties(string json)
    {
        var s = await SeedAsync();
        using var factory = new PostgresWebApplicationFactory(fixture.ConnectionString);
        using var client = Client(factory, s.Owner);
        var response = await client.PutAsync("/api/v1/users/me/settings", new StringContent(json, System.Text.Encoding.UTF8, "application/json"));
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        await using var db = fixture.CreateContext();
        Assert.False(await db.UserSettings.AnyAsync(x => x.UserId == s.Owner));
        Assert.False(await db.AuditLogs.AnyAsync(a => a.ActorUserId == s.Owner));
    }

    [Fact]
    public async Task UserPreferences_RequireAuthenticationAndHaveNoTargetUserRoute()
    {
        var s = await SeedAsync();
        using var factory = new PostgresWebApplicationFactory(fixture.ConnectionString);
        using var anonymous = factory.CreateClient();
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.GetAsync("/api/v1/users/me/settings")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.PutAsJsonAsync("/api/v1/users/me/settings", Preferences())).StatusCode);
        using var owner = Client(factory, s.Owner);
        Assert.Equal(HttpStatusCode.NotFound, (await owner.GetAsync($"/api/v1/users/{s.Member}/settings")).StatusCode);
    }

    [Theory]
    [InlineData(true, true, "project", HttpStatusCode.OK)]
    [InlineData(false, true, "platform", HttpStatusCode.Conflict)]
    [InlineData(true, false, "organization", HttpStatusCode.Conflict)]
    [InlineData(false, false, "platform", HttpStatusCode.Conflict)]
    public async Task Policy_PrecedenceAndDirectApiEnforcementAreAtomic(bool platform, bool organization, string source, HttpStatusCode expected)
    {
        var s = await SeedAsync();
        using var factory = new PostgresWebApplicationFactory(fixture.ConnectionString);
        using var host = factory.WithWebHostBuilder(b => b.ConfigureServices(services => services.AddSingleton<IPlatformCapabilities>(new Capabilities(platform))));
        using var client = host.CreateClient(); client.DefaultRequestHeaders.Add(TestAuthHandler.UserIdHeader, s.Owner.ToString());
        Assert.Equal(HttpStatusCode.OK, (await client.PutAsJsonAsync($"/api/v1/organizations/{s.Organization}/settings", new { aiAdvisoryAllowed = organization, version = 1 })).StatusCode);
        var path = $"/api/v1/projects/{s.Project}/settings";
        var response = await client.PutAsJsonAsync(path, ProjectInput(true));
        Assert.Equal(expected, response.StatusCode);
        var data = await Data(await client.GetAsync(path));
        Assert.Equal(source, data.GetProperty("effectiveAiAdvisory").GetProperty("sourceScope").GetString());
        Assert.Equal(expected == HttpStatusCode.OK, data.GetProperty("effectiveAiAdvisory").GetProperty("value").GetBoolean());
        Assert.Equal(expected == HttpStatusCode.OK ? 2 : 1, data.GetProperty("version").GetInt64());
        Assert.Equal("1", data.GetProperty("analysisProfileVersion").GetString());
        Assert.Single(data.GetProperty("availableAnalysisProfiles").EnumerateArray());
        await using var db = fixture.CreateContext();
        Assert.Equal(expected == HttpStatusCode.OK ? 1 : 0, await db.AuditLogs.CountAsync(a => a.ProjectId == s.Project));
        var settings = await db.ProjectSettings.SingleAsync(x => x.ProjectId == s.Project);
        Assert.Equal(expected == HttpStatusCode.OK ? "internal" : "private", settings.DefaultAssetVisibility);
    }

    [Fact]
    public async Task OrganizationPolicy_RolesTenancyConcurrencyAndCurrentProjectRead()
    {
        var s = await SeedAsync();
        using var factory = new PostgresWebApplicationFactory(fixture.ConnectionString);
        using var host = factory.WithWebHostBuilder(b => b.ConfigureServices(services => services.AddSingleton<IPlatformCapabilities>(new Capabilities(true))));
        using var owner = host.CreateClient(); owner.DefaultRequestHeaders.Add(TestAuthHandler.UserIdHeader, s.Owner.ToString());
        using var member = host.CreateClient(); member.DefaultRequestHeaders.Add(TestAuthHandler.UserIdHeader, s.Member.ToString());
        using var outsider = host.CreateClient(); outsider.DefaultRequestHeaders.Add(TestAuthHandler.UserIdHeader, s.Outsider.ToString());
        var path = $"/api/v1/organizations/{s.Organization}/settings";
        var initialization = await Task.WhenAll(owner.GetAsync(path), owner.GetAsync(path));
        foreach (var response in initialization) Assert.Equal(1, (await Data(response)).GetProperty("version").GetInt64());
        Assert.Equal(HttpStatusCode.Forbidden, (await member.GetAsync(path)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await member.PutAsJsonAsync(path, new { aiAdvisoryAllowed = false, version = 1 })).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await outsider.GetAsync(path)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await outsider.PutAsJsonAsync(path, new { aiAdvisoryAllowed = false, version = 1 })).StatusCode);
        var projectPath = $"/api/v1/projects/{s.Project}/settings";
        Assert.Equal(HttpStatusCode.OK, (await owner.PutAsJsonAsync(projectPath, ProjectInput(true))).StatusCode);
        var writes = await Task.WhenAll(owner.PutAsJsonAsync(path, new { aiAdvisoryAllowed = false, version = 1 }), owner.PutAsJsonAsync(path, new { aiAdvisoryAllowed = false, version = 1 }));
        Assert.Single(writes, r => r.StatusCode == HttpStatusCode.OK);
        Assert.Single(writes, r => r.StatusCode == HttpStatusCode.Conflict);
        var effective = await Data(await owner.GetAsync(projectPath));
        Assert.True(effective.GetProperty("aiAdvisoryEnabled").GetBoolean());
        Assert.False(effective.GetProperty("effectiveAiAdvisory").GetProperty("value").GetBoolean());
        Assert.Equal(2, effective.GetProperty("version").GetInt64());
        Assert.Equal(HttpStatusCode.Conflict, (await owner.PutAsJsonAsync(projectPath, ProjectInput(true, 2))).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await owner.PutAsJsonAsync(projectPath, ProjectInput(false, 2))).StatusCode);
        await using var db = fixture.CreateContext();
        Assert.Equal(1, await db.AuditLogs.CountAsync(a => a.EventType == "organization.settings_updated" && a.ActorUserId == s.Owner));
    }

    [Fact]
    public async Task Profiles_UnknownAndWrongVersionsAreRejectedAfterAuthorizationAndConcurrency()
    {
        var s = await SeedAsync();
        using var factory = new PostgresWebApplicationFactory(fixture.ConnectionString);
        using var owner = Client(factory, s.Owner);
        using var viewer = Client(factory, s.Member);
        var path = $"/api/v1/projects/{s.Project}/settings";
        foreach (var profile in new[] { "unknown", "current-default-v2", "health_overview", "commit_review", "architecture_review", $"tenant-{s.Organization}" })
            Assert.Equal(HttpStatusCode.BadRequest, (await owner.PutAsJsonAsync(path, ProjectInput(false, 1, profile))).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await viewer.PutAsJsonAsync(path, ProjectInput(false))).StatusCode);
        var viewerRead = await Data(await viewer.GetAsync(path));
        Assert.False(viewerRead.GetProperty("settingsEditable").GetBoolean());
        Assert.False(viewerRead.GetProperty("effectiveAiAdvisory").GetProperty("editable").GetBoolean());
        Assert.Equal(HttpStatusCode.OK, (await owner.PutAsJsonAsync(path, ProjectInput(false))).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await owner.PutAsJsonAsync(path, ProjectInput(false))).StatusCode);
        await using var db = fixture.CreateContext();
        Assert.Equal(1, await db.AuditLogs.CountAsync(a => a.ProjectId == s.Project));
    }

    private static HttpClient Client(PostgresWebApplicationFactory factory, Guid userId)
    {
        var client = factory.CreateClient(); client.DefaultRequestHeaders.Add(TestAuthHandler.UserIdHeader, userId.ToString()); return client;
    }

    [Theory]
    [InlineData("{\"aiAdvisoryAllowed\":false,\"version\":1,\"platformAiAdvisoryAllowed\":true}")]
    [InlineData("{\"aiAdvisoryAllowed\":false}")]
    [InlineData("{\"aiAdvisoryAllowed\":false,\"version\":0}")]
    public async Task OrganizationPolicy_RejectsOverpostingMissingAndInvalidVersionsWithoutPersistence(string json)
    {
        var s = await SeedAsync();
        using var factory = new PostgresWebApplicationFactory(fixture.ConnectionString);
        using var owner = Client(factory, s.Owner);
        var response = await owner.PutAsync($"/api/v1/organizations/{s.Organization}/settings",
            new StringContent(json, System.Text.Encoding.UTF8, "application/json"));
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        await using var db = fixture.CreateContext();
        Assert.False(await db.OrganizationSettings.AnyAsync(p => p.OrganizationId == s.Organization));
        Assert.False(await db.AuditLogs.AnyAsync(a => a.ActorUserId == s.Owner));
    }

    [Fact]
    public async Task Preferences_SeparateActorsCannotChangeEachOthersValues()
    {
        var s = await SeedAsync();
        using var factory = new PostgresWebApplicationFactory(fixture.ConnectionString);
        using var owner = Client(factory, s.Owner);
        using var member = Client(factory, s.Member);
        Assert.Equal(HttpStatusCode.OK, (await owner.PutAsJsonAsync("/api/v1/users/me/settings", Preferences())).StatusCode);
        var personal = await Data(await member.GetAsync("/api/v1/users/me/settings"));
        Assert.Equal("light", personal.GetProperty("theme").GetString());
        Assert.Equal(1, personal.GetProperty("version").GetInt64());
        var original = await Data(await owner.GetAsync("/api/v1/users/me/settings"));
        Assert.Equal("dark", original.GetProperty("theme").GetString());
        Assert.Equal(2, original.GetProperty("version").GetInt64());
    }

    [Theory]
    [InlineData("platformAiAdvisoryAllowed")]
    [InlineData("organizationId")]
    public async Task ProjectSettings_RejectsUnknownPolicyAndTenantPropertiesWithoutPartialUpdate(string field)
    {
        var s = await SeedAsync();
        using var factory = new PostgresWebApplicationFactory(fixture.ConnectionString);
        using var owner = Client(factory, s.Owner);
        var body = new Dictionary<string, object>
        {
            ["analysisProfileKey"] = "current-default-v1", ["aiAdvisoryEnabled"] = false,
            ["defaultAssetVisibility"] = "internal", ["notificationPolicyVersion"] = 1, ["version"] = 1,
            [field] = true
        };
        Assert.Equal(HttpStatusCode.BadRequest, (await owner.PutAsJsonAsync($"/api/v1/projects/{s.Project}/settings", body)).StatusCode);
        await using var db = fixture.CreateContext();
        var row = await db.ProjectSettings.SingleAsync(p => p.ProjectId == s.Project);
        Assert.Equal(1, row.Version); Assert.Equal("private", row.DefaultAssetVisibility);
        Assert.False(await db.AuditLogs.AnyAsync(a => a.ProjectId == s.Project));
    }

    [Fact]
    public async Task OrganizationPolicy_RemovedAdminLosesPolicyAccessAndNeverGainsProjectSettingsPermission()
    {
        var s = await SeedAsync();
        using var factory = new PostgresWebApplicationFactory(fixture.ConnectionString);
        using var owner = Client(factory, s.Owner);
        using var admin = Client(factory, s.Member);
        Assert.Equal(HttpStatusCode.OK, (await owner.PatchAsJsonAsync($"/api/v1/organizations/{s.Organization}/members/{s.Member}",
            new { role = "organizationAdmin", status = "active", version = 1 })).StatusCode);
        var path = $"/api/v1/organizations/{s.Organization}/settings";
        Assert.Equal(HttpStatusCode.OK, (await admin.PutAsJsonAsync(path, new { aiAdvisoryAllowed = false, version = 1 })).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await admin.PutAsJsonAsync($"/api/v1/projects/{s.Project}/settings", ProjectInput(false))).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await owner.DeleteAsync($"/api/v1/organizations/{s.Organization}/members/{s.Member}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await admin.GetAsync(path)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await admin.PutAsJsonAsync(path, new { aiAdvisoryAllowed = true, version = 2 })).StatusCode);
        await using var db = fixture.CreateContext();
        var persisted = await db.OrganizationSettings.SingleAsync(p => p.OrganizationId == s.Organization);
        Assert.False(persisted.AiAdvisoryAllowed);
        Assert.Equal(2, persisted.Version);
    }

    [Theory]
    [InlineData("archived")]
    [InlineData("deleting")]
    [InlineData("organization-deleting")]
    public async Task WorkerPolicy_CurrentLifecyclePreventsQueuedAi(string state)
    {
        var s = await SeedAsync();
        await using var db = fixture.CreateContext();
        var settings = await db.ProjectSettings.SingleAsync(p => p.ProjectId == s.Project);
        settings.Update("current-default-v1", true, "private", 1, 1, Now);
        await db.SaveChangesAsync();
        var policy = new AiExecutionPolicy(new AiExecutionStateReader(db), new EffectiveSettingsPolicy(new Capabilities(true), new OrganizationSettingRepository(db)));
        Assert.True(await policy.IsAllowedAsync(s.Project, CancellationToken.None));
        await using var other = fixture.CreateContext();
        if (state == "organization-deleting")
            (await other.Organizations.SingleAsync(o => o.Id == s.Organization)).MarkDeleting(1, Now);
        else
        {
            var project = await other.Projects.SingleAsync(p => p.Id == s.Project);
            if (state == "archived") project.Archive(1, Now);
            else project.MarkDeleting(1, Now);
        }
        await other.SaveChangesAsync();
        Assert.False(await policy.IsAllowedAsync(s.Project, CancellationToken.None));
    }

    [Fact]
    public async Task Preferences_AuditFailureRollsBackInitializationAndMutation()
    {
        var s = await SeedAsync();
        var audit = new Mock<IAuditWriter>();
        audit.Setup(a => a.WriteAuditAsync(It.IsAny<Guid?>(), It.IsAny<Guid?>(), It.IsAny<string>(), It.IsAny<string?>(),
                It.IsAny<Guid?>(), It.IsAny<string>(), It.IsAny<object?>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("audit unavailable"));
        using var factory = new PostgresWebApplicationFactory(fixture.ConnectionString);
        using var host = factory.WithWebHostBuilder(b => b.ConfigureServices(services => services.AddScoped(_ => audit.Object)));
        using var owner = host.CreateClient(); owner.DefaultRequestHeaders.Add(TestAuthHandler.UserIdHeader, s.Owner.ToString());
        Assert.Equal(HttpStatusCode.InternalServerError, (await owner.PutAsJsonAsync("/api/v1/users/me/settings", Preferences())).StatusCode);
        await using var db = fixture.CreateContext();
        Assert.False(await db.UserSettings.AnyAsync(x => x.UserId == s.Owner));
        Assert.False(await db.AuditLogs.AnyAsync(x => x.ActorUserId == s.Owner));
    }

    [Fact]
    public async Task ConcurrentProjectEnableAndOrganizationRestrictionAlwaysResolveToRestriction()
    {
        var s = await SeedAsync();
        using var factory = new PostgresWebApplicationFactory(fixture.ConnectionString);
        using var host = factory.WithWebHostBuilder(b => b.ConfigureServices(services => services.AddSingleton<IPlatformCapabilities>(new Capabilities(true))));
        using var owner = host.CreateClient(); owner.DefaultRequestHeaders.Add(TestAuthHandler.UserIdHeader, s.Owner.ToString());
        var projectPath = $"/api/v1/projects/{s.Project}/settings";
        var writes = await Task.WhenAll(owner.PutAsJsonAsync(projectPath, ProjectInput(true)),
            owner.PutAsJsonAsync($"/api/v1/organizations/{s.Organization}/settings", new { aiAdvisoryAllowed = false, version = 1 }));
        Assert.Equal(HttpStatusCode.OK, writes[1].StatusCode);
        Assert.Contains(writes[0].StatusCode, new[] { HttpStatusCode.OK, HttpStatusCode.Conflict });
        var effective = await Data(await owner.GetAsync(projectPath));
        Assert.False(effective.GetProperty("effectiveAiAdvisory").GetProperty("value").GetBoolean());
        Assert.Equal("organization", effective.GetProperty("effectiveAiAdvisory").GetProperty("sourceScope").GetString());
        await using var db = fixture.CreateContext();
        Assert.Equal(writes[0].IsSuccessStatusCode ? 1 : 0, await db.AuditLogs.CountAsync(a => a.ProjectId == s.Project));
    }

    [Fact]
    public async Task WorkerPolicy_RechecksCurrentStateWithinSameScopeAndAdminCanRestrict()
    {
        var s = await SeedAsync();
        using var factory = new PostgresWebApplicationFactory(fixture.ConnectionString);
        using var host = factory.WithWebHostBuilder(b => b.ConfigureServices(services => services.AddSingleton<IPlatformCapabilities>(new Capabilities(true))));
        using var owner = host.CreateClient(); owner.DefaultRequestHeaders.Add(TestAuthHandler.UserIdHeader, s.Owner.ToString());
        Assert.Equal(HttpStatusCode.OK, (await owner.PutAsJsonAsync($"/api/v1/projects/{s.Project}/settings", ProjectInput(true))).StatusCode);
        await using var db = fixture.CreateContext();
        var resolver = new EffectiveSettingsPolicy(new Capabilities(true), new OrganizationSettingRepository(db));
        var execution = new AiExecutionPolicy(new AiExecutionStateReader(db), resolver);
        Assert.True(await execution.IsAllowedAsync(s.Project, CancellationToken.None));
        using var member = host.CreateClient(); member.DefaultRequestHeaders.Add(TestAuthHandler.UserIdHeader, s.Member.ToString());
        Assert.Equal(HttpStatusCode.OK, (await owner.PatchAsJsonAsync($"/api/v1/organizations/{s.Organization}/members/{s.Member}",
            new { role = "organizationAdmin", status = "active", version = 1 })).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await member.PutAsJsonAsync($"/api/v1/organizations/{s.Organization}/settings", new { aiAdvisoryAllowed = false, version = 1 })).StatusCode);
        Assert.False(await execution.IsAllowedAsync(s.Project, CancellationToken.None));
        Assert.Equal(HttpStatusCode.OK, (await member.PutAsJsonAsync($"/api/v1/organizations/{s.Organization}/settings", new { aiAdvisoryAllowed = true, version = 2 })).StatusCode);
        Assert.True(await execution.IsAllowedAsync(s.Project, CancellationToken.None));
        Assert.Equal(HttpStatusCode.OK, (await owner.PutAsJsonAsync($"/api/v1/projects/{s.Project}/settings", ProjectInput(false, 2))).StatusCode);
        Assert.False(await execution.IsAllowedAsync(s.Project, CancellationToken.None));
    }
    private static async Task<JsonElement> Data(HttpResponseMessage response)
    {
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return json.RootElement.GetProperty("data").Clone();
    }
    private sealed record Capabilities(bool AiAdvisoryAllowed) : IPlatformCapabilities;
    private async Task<(Guid Owner, Guid Member, Guid Outsider, Guid Organization, Guid Project)> SeedAsync()
    {
        await using var db = fixture.CreateContext();
        var suffix = Guid.NewGuid().ToString("N");
        var users = Enumerable.Range(0, 3).Select(i => User.Create($"settings-{i}-{suffix}@example.test", $"User {i}", "hash", Now)).ToArray();
        foreach (var user in users) user.MarkEmailVerified(Now);
        db.Users.AddRange(users);
        var org = Organization.Create($"Settings {suffix}", $"settings-{suffix}", users[0].Id, Now);
        db.Organizations.Add(org);
        db.OrganizationMembers.Add(OrganizationMember.Create(org.Id, users[0].Id, OrganizationRole.OrganizationOwner, users[0].Id, Now));
        db.OrganizationMembers.Add(OrganizationMember.Create(org.Id, users[1].Id, OrganizationRole.OrganizationMember, users[0].Id, Now));
        var project = Project.Create(org.Id, $"Project {suffix}", $"project-{suffix}", null, "unknown", ProjectVisibility.Private, users[0].Id, Now);
        db.Projects.Add(project); db.ProjectSettings.Add(ProjectSetting.Create(project.Id, Now));
        db.ProjectMembers.Add(ProjectMember.Create(project.Id, org.Id, users[0].Id, ProjectRole.ProjectOwner, ProjectMemberSource.Direct, users[0].Id, Now));
        db.ProjectMembers.Add(ProjectMember.Create(project.Id, org.Id, users[1].Id, ProjectRole.Viewer, ProjectMemberSource.Direct, users[0].Id, Now));
        await db.SaveChangesAsync();
        return (users[0].Id, users[1].Id, users[2].Id, org.Id, project.Id);
    }
}
