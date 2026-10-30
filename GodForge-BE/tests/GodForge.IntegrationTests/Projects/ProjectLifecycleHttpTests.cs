using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using GodForge.Domain.Entities.Core;
using GodForge.Domain.Entities.Identity;
using GodForge.Domain.Enums;
using GodForge.Infrastructure.Persistence;
using GodForge.IntegrationTests.Infrastructure;
using GodForge.IntegrationTests.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GodForge.IntegrationTests.Projects;

[Collection(PostgresPersistenceCollection.CollectionName)]
public sealed class ProjectLifecycleHttpTests(PostgresPersistenceFixture fixture)
{
    private static readonly DateTimeOffset Now = new(2026, 10, 26, 0, 0, 0, TimeSpan.Zero);

    [Theory]
    [InlineData("owner", HttpStatusCode.OK)]
    [InlineData("maintainer", HttpStatusCode.OK)]
    [InlineData("developer", HttpStatusCode.Forbidden)]
    [InlineData("organizationAdmin", HttpStatusCode.Forbidden)]
    [InlineData("outsider", HttpStatusCode.NotFound)]
    public async Task Archive_EnforcesProjectRoleAndTenantScope(string actor, HttpStatusCode expected)
    {
        var seed = await SeedAsync();
        using var factory = new PostgresWebApplicationFactory(fixture.ConnectionString);
        using var client = CreateClient(factory, seed.Users[actor]);
        var response = await client.PostAsJsonAsync($"/api/v1/projects/{seed.ProjectId}/archive", new { version = 1 });
        Assert.Equal(expected, response.StatusCode);
        await using var context = fixture.CreateContext();
        var project = await context.Projects.SingleAsync(p => p.Id == seed.ProjectId);
        Assert.Equal(expected == HttpStatusCode.OK ? ProjectStatus.Archived : ProjectStatus.Active, project.Status);
        Assert.Equal(expected == HttpStatusCode.OK ? 2 : 1, project.Version);
        Assert.Equal(expected == HttpStatusCode.OK ? 1 : 0,
            await context.AuditLogs.CountAsync(a => a.ProjectId == seed.ProjectId && a.EventType == "project.archived"));
    }

    [Theory]
    [InlineData("add")]
    [InlineData("role")]
    [InlineData("remove")]
    [InlineData("ownership")]
    [InlineData("settings")]
    [InlineData("metadata")]
    public async Task ArchivedProject_RejectsMutationsWithoutChangingMembershipsSettingsOrAudit(string operation)
    {
        var seed = await SeedAsync(archived: true);
        using var factory = new PostgresWebApplicationFactory(fixture.ConnectionString);
        using var client = CreateClient(factory, seed.Users["owner"]);
        var prefix = $"/api/v1/projects/{seed.ProjectId}";
        var response = operation switch
        {
            "add" => await client.PostAsJsonAsync($"{prefix}/members", new { userId = seed.Users["candidate"], role = "viewer" }),
            "role" => await client.PatchAsJsonAsync($"{prefix}/members/{seed.Users["developer"]}", new { role = "reviewer", version = 1 }),
            "remove" => await client.DeleteAsync($"{prefix}/members/{seed.Users["developer"]}"),
            "ownership" => await client.PostAsJsonAsync($"{prefix}/transfer-ownership", new
            {
                newOwnerUserId = seed.Users["maintainer"],
                retainCurrentOwnerAs = "maintainer",
                version = 2
            }),
            "settings" => await client.PutAsJsonAsync($"{prefix}/settings", new
            {
                analysisProfileKey = "default",
                aiAdvisoryEnabled = false,
                defaultAssetVisibility = "private",
                notificationPolicyVersion = 1,
                version = 1
            }),
            _ => await client.PatchAsJsonAsync(prefix, new
            {
                name = "Changed",
                slug = "changed",
                description = "Changed",
                visibility = "private",
                version = 2
            })
        };
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("PROJECT_ARCHIVED", body.RootElement.GetProperty("error").GetProperty("code").GetString());
        await using var context = fixture.CreateContext();
        Assert.Equal(3, await context.ProjectMembers.CountAsync(m => m.ProjectId == seed.ProjectId));
        Assert.All(await context.ProjectMembers.Where(m => m.ProjectId == seed.ProjectId).ToListAsync(),
            membership => { Assert.Equal(1, membership.Version); Assert.Equal(MembershipStatus.Active, membership.Status); });
        Assert.Equal(1, (await context.ProjectSettings.SingleAsync(s => s.ProjectId == seed.ProjectId)).Version);
        Assert.False(await context.AuditLogs.AnyAsync(a => a.ProjectId == seed.ProjectId));
    }

    [Fact]
    public async Task ArchiveAndRestore_ConsumeVersionsAndPreserveRemovedMemberships()
    {
        var seed = await SeedAsync();
        using var factory = new PostgresWebApplicationFactory(fixture.ConnectionString);
        using var client = CreateClient(factory, seed.Users["owner"]);
        var prefix = $"/api/v1/projects/{seed.ProjectId}";
        Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync($"{prefix}/members/{seed.Users["developer"]}")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.PostAsJsonAsync($"{prefix}/archive", new { version = 1 })).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await client.PostAsJsonAsync($"{prefix}/restore", new { version = 1 })).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync(prefix)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.PostAsJsonAsync($"{prefix}/restore", new { version = 2 })).StatusCode);
        await using var context = fixture.CreateContext();
        var project = await context.Projects.SingleAsync(p => p.Id == seed.ProjectId);
        Assert.Equal(ProjectStatus.Active, project.Status);
        Assert.Equal(3, project.Version);
        Assert.Equal(MembershipStatus.Removed, (await context.ProjectMembers.SingleAsync(m =>
            m.ProjectId == seed.ProjectId && m.UserId == seed.Users["developer"])).Status);
        Assert.Equal(2, await context.AuditLogs.CountAsync(a => a.ProjectId == seed.ProjectId &&
            (a.EventType == "project.archived" || a.EventType == "project.restored")));
    }

    [Fact]
    public async Task ProjectOwnershipTransfer_ConsumesAggregateVersionAndRejectsReuse()
    {
        var seed = await SeedAsync();
        using var factory = new PostgresWebApplicationFactory(fixture.ConnectionString);
        using var owner = CreateClient(factory, seed.Users["owner"]);
        var route = $"/api/v1/projects/{seed.ProjectId}/transfer-ownership";
        Assert.Equal(HttpStatusCode.OK, (await owner.PostAsJsonAsync(route, new
        {
            newOwnerUserId = seed.Users["maintainer"],
            retainCurrentOwnerAs = "maintainer",
            version = 1
        })).StatusCode);
        using var newOwner = CreateClient(factory, seed.Users["maintainer"]);
        Assert.Equal(HttpStatusCode.Conflict, (await newOwner.PostAsJsonAsync(route, new
        {
            newOwnerUserId = seed.Users["owner"],
            retainCurrentOwnerAs = "maintainer",
            version = 1
        })).StatusCode);
        await using var context = fixture.CreateContext();
        Assert.Equal(2, (await context.Projects.SingleAsync(p => p.Id == seed.ProjectId)).Version);
        Assert.Equal(ProjectRole.ProjectOwner, (await context.ProjectMembers.SingleAsync(m =>
            m.ProjectId == seed.ProjectId && m.UserId == seed.Users["maintainer"])).Role);
        Assert.Equal(1, await context.AuditLogs.CountAsync(a => a.ProjectId == seed.ProjectId && a.EventType == "project.ownership_transferred"));
    }

    [Fact]
    public async Task OrganizationOwnershipTransfer_ConsumesAggregateVersionAndRejectsReuse()
    {
        var seed = await SeedAsync();
        using var factory = new PostgresWebApplicationFactory(fixture.ConnectionString);
        using var owner = CreateClient(factory, seed.Users["owner"]);
        var route = $"/api/v1/organizations/{seed.OrganizationId}/transfer-ownership";
        Assert.Equal(HttpStatusCode.OK, (await owner.PostAsJsonAsync(route, new
        {
            newOwnerUserId = seed.Users["maintainer"],
            retainCurrentOwnerAs = "organizationAdmin",
            version = 1
        })).StatusCode);
        using var newOwner = CreateClient(factory, seed.Users["maintainer"]);
        Assert.Equal(HttpStatusCode.Conflict, (await newOwner.PostAsJsonAsync(route, new
        {
            newOwnerUserId = seed.Users["owner"],
            retainCurrentOwnerAs = "organizationAdmin",
            version = 1
        })).StatusCode);
        await using var context = fixture.CreateContext();
        Assert.Equal(2, (await context.Organizations.SingleAsync(o => o.Id == seed.OrganizationId)).Version);
        Assert.Equal(1, await context.AuditLogs.CountAsync(a => a.ResourceId == seed.OrganizationId && a.EventType == "organization.ownership_transferred"));
    }

    private static HttpClient CreateClient(PostgresWebApplicationFactory factory, Guid actorId)
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthHandler.UserIdHeader, actorId.ToString());
        return client;
    }

    [Fact]
    public async Task ConcurrentOwnershipTransfers_ConsumeVersionOnlyOnceEvenWithTwoOwners()
    {
        var seed = await SeedAsync();
        await using (var context = fixture.CreateContext())
        {
            context.ProjectMembers.Add(ProjectMember.Create(seed.ProjectId, seed.OrganizationId,
                seed.Users["candidate"], ProjectRole.ProjectOwner, ProjectMemberSource.Direct, seed.Users["owner"], Now));
            await context.SaveChangesAsync();
        }
        using var factory = new PostgresWebApplicationFactory(fixture.ConnectionString);
        using var first = CreateClient(factory, seed.Users["owner"]);
        using var second = CreateClient(factory, seed.Users["candidate"]);
        var route = $"/api/v1/projects/{seed.ProjectId}/transfer-ownership";
        var responses = await Task.WhenAll(
            first.PostAsJsonAsync(route, new { newOwnerUserId = seed.Users["maintainer"], retainCurrentOwnerAs = "maintainer", version = 1 }),
            second.PostAsJsonAsync(route, new { newOwnerUserId = seed.Users["developer"], retainCurrentOwnerAs = "maintainer", version = 1 }));
        Assert.Single(responses, r => r.StatusCode == HttpStatusCode.OK);
        Assert.Single(responses, r => r.StatusCode == HttpStatusCode.Conflict);
        await using var verification = fixture.CreateContext();
        Assert.Equal(2, (await verification.Projects.SingleAsync(p => p.Id == seed.ProjectId)).Version);
        Assert.Equal(1, await verification.AuditLogs.CountAsync(a => a.ProjectId == seed.ProjectId && a.EventType == "project.ownership_transferred"));
    }

    [Fact]
    public async Task ConcurrentArchive_ChangesStateAndAuditsExactlyOnce()
    {
        var seed = await SeedAsync();
        using var factory = new PostgresWebApplicationFactory(fixture.ConnectionString);
        using var first = CreateClient(factory, seed.Users["owner"]);
        using var second = CreateClient(factory, seed.Users["maintainer"]);
        var route = $"/api/v1/projects/{seed.ProjectId}/archive";
        var responses = await Task.WhenAll(first.PostAsJsonAsync(route, new { version = 1 }), second.PostAsJsonAsync(route, new { version = 1 }));
        Assert.Single(responses, r => r.StatusCode == HttpStatusCode.OK);
        Assert.Single(responses, r => r.StatusCode == HttpStatusCode.Conflict);
        await using var context = fixture.CreateContext();
        Assert.Equal(2, (await context.Projects.SingleAsync(p => p.Id == seed.ProjectId)).Version);
        Assert.Equal(1, await context.AuditLogs.CountAsync(a => a.ProjectId == seed.ProjectId && a.EventType == "project.archived"));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task AddMember_WaitsForOrganizationRevocationAndRechecksCurrentMembership(bool revokeActor)
    {
        var seed = await SeedAsync();
        using var factory = new PostgresWebApplicationFactory(fixture.ConnectionString);
        using var client = CreateClient(factory, seed.Users[revokeActor ? "maintainer" : "owner"]);
        await using var context = fixture.CreateContext();
        var unit = new UnitOfWork(context);
        await unit.BeginTransactionAsync();
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        try
        {
            await unit.AcquireResourceLockAsync("organization-membership", seed.OrganizationId);
            var addition = client.PostAsJsonAsync($"/api/v1/projects/{seed.ProjectId}/members",
                new { userId = seed.Users["candidate"], role = "viewer" }, timeout.Token);
            var blocked = false;
            var deadline = DateTime.UtcNow.AddSeconds(5);
            while (!addition.IsCompleted && DateTime.UtcNow < deadline)
            {
                await using var command = context.Database.GetDbConnection().CreateCommand();
                command.CommandText = """
                    SELECT EXISTS (
                        SELECT 1 FROM pg_locks waiting JOIN pg_locks held
                          ON waiting.locktype = held.locktype AND waiting.classid = held.classid
                          AND waiting.objid = held.objid AND waiting.objsubid = held.objsubid
                          AND waiting.database = held.database
                        WHERE held.pid = pg_backend_pid() AND held.locktype = 'advisory'
                          AND held.granted AND NOT waiting.granted)
                    """;
                if (await command.ExecuteScalarAsync(timeout.Token) is true) { blocked = true; break; }
                await Task.Delay(20, timeout.Token);
            }
            Assert.True(blocked, "Project member addition must wait for the organization membership transaction.");
            var revokedUserId = seed.Users[revokeActor ? "maintainer" : "candidate"];
            var candidate = await context.OrganizationMembers.SingleAsync(m => m.OrganizationId == seed.OrganizationId && m.UserId == revokedUserId);
            candidate.Change(candidate.Role, MembershipStatus.Removed, seed.Users["owner"], candidate.Version, Now);
            await unit.SaveChangesAsync();
            await unit.CommitTransactionAsync();
            Assert.Equal(HttpStatusCode.NotFound, (await addition).StatusCode);
            await using var verification = fixture.CreateContext();
            Assert.False(await verification.ProjectMembers.AnyAsync(m => m.ProjectId == seed.ProjectId && m.UserId == seed.Users["candidate"]));
        }
        finally
        {
            await unit.RollbackTransactionAsync();
            await timeout.CancelAsync();
        }
    }

    [Fact]
    public async Task Archive_InvalidVersion_ReturnsValidationWithoutChangingState()
    {
        var seed = await SeedAsync();
        using var factory = new PostgresWebApplicationFactory(fixture.ConnectionString);
        using var client = CreateClient(factory, seed.Users["owner"]);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync($"/api/v1/projects/{seed.ProjectId}/archive", new { version = 0 })).StatusCode);
        await using var context = fixture.CreateContext();
        Assert.Equal(ProjectStatus.Active, (await context.Projects.SingleAsync(p => p.Id == seed.ProjectId)).Status);
        Assert.False(await context.AuditLogs.AnyAsync(a => a.ProjectId == seed.ProjectId));
    }

    [Theory]
    [InlineData("active", "active", "active", HttpStatusCode.OK)]
    [InlineData("suspended", "active", "active", HttpStatusCode.NotFound)]
    [InlineData("removed", "active", "active", HttpStatusCode.NotFound)]
    [InlineData("active", "suspended", "active", HttpStatusCode.NotFound)]
    [InlineData("active", "removed", "active", HttpStatusCode.NotFound)]
    [InlineData("active", "active", "disabled", HttpStatusCode.NotFound)]
    [InlineData("active", "active", "locked", HttpStatusCode.NotFound)]
    public async Task Ownership_RequiresActiveAccountAndBothMemberships(string projectStatus, string orgStatus, string userStatus, HttpStatusCode expected)
    {
        var seed = await SeedAsync();
        var target = seed.Users["developer"];
        await using (var db = fixture.CreateContext())
        {
            await db.ProjectMembers.Where(m => m.ProjectId == seed.ProjectId && m.UserId == target)
                .ExecuteUpdateAsync(s => s.SetProperty(m => m.Status, Enum.Parse<MembershipStatus>(projectStatus, true)));
            await db.OrganizationMembers.Where(m => m.OrganizationId == seed.OrganizationId && m.UserId == target)
                .ExecuteUpdateAsync(s => s.SetProperty(m => m.Status, Enum.Parse<MembershipStatus>(orgStatus, true)));
            await db.Users.Where(u => u.Id == target).ExecuteUpdateAsync(s => s.SetProperty(u => u.Status, Enum.Parse<UserStatus>(userStatus, true)));
        }
        using var factory = new PostgresWebApplicationFactory(fixture.ConnectionString);
        using var client = CreateClient(factory, seed.Users["owner"]);
        var response = await client.PostAsJsonAsync($"/api/v1/projects/{seed.ProjectId}/transfer-ownership", new { newOwnerUserId = target, retainCurrentOwnerAs = "maintainer", version = 1 });
        Assert.Equal(expected, response.StatusCode);
        await using var verification = fixture.CreateContext();
        Assert.Equal(expected == HttpStatusCode.OK ? 2 : 1, (await verification.Projects.SingleAsync(p => p.Id == seed.ProjectId)).Version);
        Assert.Equal(expected == HttpStatusCode.OK ? ProjectRole.ProjectOwner : ProjectRole.Developer,
            (await verification.ProjectMembers.SingleAsync(m => m.ProjectId == seed.ProjectId && m.UserId == target)).Role);
        Assert.Equal(expected == HttpStatusCode.OK ? 1 : 0, await verification.AuditLogs.CountAsync(a => a.ProjectId == seed.ProjectId));
    }

    [Theory]
    [InlineData("owner", "developer", HttpStatusCode.Created)]
    [InlineData("owner", "viewer", HttpStatusCode.Created)]
    [InlineData("owner", "maintainer", HttpStatusCode.Created)]
    [InlineData("maintainer", "maintainer", HttpStatusCode.Created)]
    [InlineData("maintainer", "projectOwner", HttpStatusCode.Forbidden)]
    [InlineData("developer", "viewer", HttpStatusCode.Forbidden)]
    [InlineData("organizationAdmin", "viewer", HttpStatusCode.Forbidden)]
    [InlineData("outsider", "viewer", HttpStatusCode.NotFound)]
    public async Task MemberAddition_EnforcesGrantBoundary(string actor, string role, HttpStatusCode expected)
    {
        var seed = await SeedAsync();
        using var factory = new PostgresWebApplicationFactory(fixture.ConnectionString);
        using var client = CreateClient(factory, seed.Users[actor]);
        var response = await client.PostAsJsonAsync($"/api/v1/projects/{seed.ProjectId}/members", new { userId = seed.Users["candidate"], role });
        Assert.Equal(expected, response.StatusCode);
        await using var db = fixture.CreateContext();
        Assert.Equal(expected == HttpStatusCode.Created, await db.ProjectMembers.AnyAsync(m => m.ProjectId == seed.ProjectId && m.UserId == seed.Users["candidate"]));
        Assert.Equal(expected == HttpStatusCode.Created ? 1 : 0, await db.AuditLogs.CountAsync(a => a.ProjectId == seed.ProjectId));
    }

    [Fact]
    public async Task MemberAddition_IdempotentReplayDoesNotDuplicateOrReactivateRevokedMembership()
    {
        var seed = await SeedAsync();
        using var factory = new PostgresWebApplicationFactory(fixture.ConnectionString);
        using var client = CreateClient(factory, seed.Users["owner"]);
        var prefix = $"/api/v1/projects/{seed.ProjectId}";
        client.DefaultRequestHeaders.Add("Idempotency-Key", Guid.NewGuid().ToString());
        var input = new { userId = seed.Users["candidate"], role = "developer" };
        var results = await Task.WhenAll(client.PostAsJsonAsync($"{prefix}/members", input), client.PostAsJsonAsync($"{prefix}/members", input));
        Assert.All(results, r => Assert.Equal(HttpStatusCode.Created, r.StatusCode));
        Assert.Equal(HttpStatusCode.Conflict, (await client.PostAsJsonAsync($"{prefix}/members", new { userId = input.userId, role = "viewer" })).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync($"{prefix}/members/{input.userId}")).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await client.PostAsJsonAsync($"{prefix}/members", input)).StatusCode);
        await using var db = fixture.CreateContext();
        Assert.Single(await db.ProjectMembers.Where(m => m.ProjectId == seed.ProjectId && m.UserId == input.userId).ToListAsync());
        Assert.Equal(MembershipStatus.Removed, (await db.ProjectMembers.SingleAsync(m => m.ProjectId == seed.ProjectId && m.UserId == input.userId)).Status);
        Assert.Equal(1, await db.AuditLogs.CountAsync(a => a.ProjectId == seed.ProjectId && a.EventType == "project.member_added"));
    }

    [Theory]
    [InlineData("settings")]
    [InlineData("roles")]
    [InlineData("add")]
    [InlineData("last-owner")]
    [InlineData("archive-settings")]
    [InlineData("archive-add")]
    [InlineData("delete-archive")]
    [InlineData("remove-transfer")]
    public async Task ConcurrentMutations_PreserveVersionsOwnersAndAudit(string operation)
    {
        var seed = await SeedAsync();
        using var factory = new PostgresWebApplicationFactory(fixture.ConnectionString);
        using var owner = CreateClient(factory, seed.Users["owner"]);
        using var maintainer = CreateClient(factory, seed.Users["maintainer"]);
        var prefix = $"/api/v1/projects/{seed.ProjectId}";
        var settings = new { analysisProfileKey = "current-default-v1", aiAdvisoryEnabled = false, defaultAssetVisibility = "internal", notificationPolicyVersion = 1, version = 1 };
        Task<HttpResponseMessage> ChangeSettings() => owner.PutAsJsonAsync($"{prefix}/settings", settings);
        Task<HttpResponseMessage> Add() => owner.PostAsJsonAsync($"{prefix}/members", new { userId = seed.Users["candidate"], role = "viewer" });
        Task<HttpResponseMessage> Role(string role) => owner.PatchAsJsonAsync($"{prefix}/members/{seed.Users["developer"]}", new { role, version = 1 });
        var requests = operation switch
        {
            "settings" => new[] { ChangeSettings(), ChangeSettings() },
            "roles" => new[] { Role("reviewer"), Role("viewer") },
            "add" => new[] { Add(), Add() },
            "last-owner" => new[] { owner.DeleteAsync($"{prefix}/members/{seed.Users["owner"]}"), owner.DeleteAsync($"{prefix}/members/{seed.Users["owner"]}") },
            "archive-settings" => new[] { maintainer.PostAsJsonAsync($"{prefix}/archive", new { version = 1 }), ChangeSettings() },
            "archive-add" => new[] { maintainer.PostAsJsonAsync($"{prefix}/archive", new { version = 1 }), Add() },
            "remove-transfer" => new[] { owner.DeleteAsync($"{prefix}/members/{seed.Users["developer"]}"), owner.PostAsJsonAsync($"{prefix}/transfer-ownership", new { newOwnerUserId = seed.Users["developer"], retainCurrentOwnerAs = "maintainer", version = 1 }) },
            _ => new[] { owner.SendAsync(new HttpRequestMessage(HttpMethod.Delete, prefix) { Content = JsonContent.Create(new { version = 1, confirmationSlug = $"project-{await GetSuffixAsync(seed.ProjectId)}" }) }), maintainer.PostAsJsonAsync($"{prefix}/archive", new { version = 1 }) }
        };
        var results = await Task.WhenAll(requests);
        Assert.All(results, response => Assert.Contains(response.StatusCode, new[] { HttpStatusCode.OK, HttpStatusCode.Created, HttpStatusCode.Accepted, HttpStatusCode.NoContent, HttpStatusCode.Conflict, HttpStatusCode.NotFound, HttpStatusCode.Forbidden }));
        await using var db = fixture.CreateContext();
        Assert.True(await db.ProjectMembers.AnyAsync(m => m.ProjectId == seed.ProjectId && m.Role == ProjectRole.ProjectOwner && m.Status == MembershipStatus.Active));
        if (operation is "settings" or "roles" or "add")
        {
            Assert.Single(results, r => r.IsSuccessStatusCode);
            Assert.Single(results, r => r.StatusCode == HttpStatusCode.Conflict);
            Assert.Equal(1, await db.AuditLogs.CountAsync(a => a.ProjectId == seed.ProjectId));
        }
        if (operation == "settings")
        {
            Assert.Equal(2, (await db.ProjectSettings.SingleAsync(s => s.ProjectId == seed.ProjectId)).Version);
            Assert.Equal(1, (await db.Projects.SingleAsync(p => p.Id == seed.ProjectId)).Version);
        }
        if (operation == "last-owner") { Assert.All(results, r => Assert.Equal(HttpStatusCode.Conflict, r.StatusCode)); Assert.False(await db.AuditLogs.AnyAsync(a => a.ProjectId == seed.ProjectId)); }
        if (operation is "archive-settings" or "archive-add")
        {
            Assert.Equal(ProjectStatus.Archived, (await db.Projects.SingleAsync(p => p.Id == seed.ProjectId)).Status);
            Assert.Equal(results.Count(r => r.IsSuccessStatusCode), await db.AuditLogs.CountAsync(a => a.ProjectId == seed.ProjectId));
        }
        if (operation == "delete-archive") { Assert.Single(results, r => r.IsSuccessStatusCode); Assert.Equal(2, (await db.Projects.SingleAsync(p => p.Id == seed.ProjectId)).Version); }
    }

    [Fact]
    public async Task MemberCandidates_BeyondHundredAreReachableWithStablePaginationAndSearch()
    {
        var seed = await SeedAsync();
        await using (var db = fixture.CreateContext())
        {
            for (var index = 0; index < 105; index++)
            {
                var user = User.Create($"scale-{Guid.NewGuid():N}@example.test", $"Scale candidate {index:000}", "hash", Now);
                user.MarkEmailVerified(Now);
                db.Users.Add(user);
                db.OrganizationMembers.Add(OrganizationMember.Create(seed.OrganizationId, user.Id, OrganizationRole.OrganizationMember, seed.Users["owner"], Now));
            }
            await db.SaveChangesAsync();
        }
        using var factory = new PostgresWebApplicationFactory(fixture.ConnectionString);
        using var client = CreateClient(factory, seed.Users["owner"]);
        var ids = new HashSet<Guid>();
        Guid candidate = Guid.Empty;
        for (var page = 1; page <= 6; page++)
        {
            using var body = JsonDocument.Parse(await client.GetStringAsync($"/api/v1/organizations/{seed.OrganizationId}/members?page={page}&pageSize=20&status=active"));
            Assert.Equal(110, body.RootElement.GetProperty("meta").GetProperty("totalCount").GetInt32());
            foreach (var row in body.RootElement.GetProperty("data").EnumerateArray())
            {
                var userId = row.GetProperty("userId").GetGuid();
                Assert.True(ids.Add(userId), "Stable pagination must not repeat members with equal joined timestamps.");
                if (page == 6 && row.GetProperty("displayName").GetString()!.StartsWith("Scale candidate", StringComparison.Ordinal)) candidate = userId;
            }
        }
        Assert.Equal(110, ids.Count);
        Assert.NotEqual(Guid.Empty, candidate);
        Assert.Equal(HttpStatusCode.Created, (await client.PostAsJsonAsync($"/api/v1/projects/{seed.ProjectId}/members", new { userId = candidate, role = "viewer" })).StatusCode);
        using var search = JsonDocument.Parse(await client.GetStringAsync($"/api/v1/organizations/{seed.OrganizationId}/members?search=Scale%20candidate%20104&status=active"));
        Assert.Single(search.RootElement.GetProperty("data").EnumerateArray());
    }

    [Theory]
    [InlineData("viewer")]
    [InlineData("reviewer")]
    [InlineData("developer")]
    public async Task NonAdministrativeRoles_CannotMutateSettingsOrRemoveOwner(string role)
    {
        var seed = await SeedAsync();
        await using (var db = fixture.CreateContext())
            await db.ProjectMembers.Where(m => m.ProjectId == seed.ProjectId && m.UserId == seed.Users["developer"])
                .ExecuteUpdateAsync(s => s.SetProperty(m => m.Role, Enum.Parse<ProjectRole>(role, true)));
        using var factory = new PostgresWebApplicationFactory(fixture.ConnectionString);
        using var client = CreateClient(factory, seed.Users["developer"]);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.DeleteAsync($"/api/v1/projects/{seed.ProjectId}/members/{seed.Users["owner"]}")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.PutAsJsonAsync($"/api/v1/projects/{seed.ProjectId}/settings", new { analysisProfileKey = "current-default-v1", aiAdvisoryEnabled = true, defaultAssetVisibility = "internal", notificationPolicyVersion = 1, version = 1 })).StatusCode);
        await using var verification = fixture.CreateContext();
        Assert.Equal(1, (await verification.ProjectSettings.SingleAsync(s => s.ProjectId == seed.ProjectId)).Version);
        Assert.False(await verification.AuditLogs.AnyAsync(a => a.ProjectId == seed.ProjectId));
    }

    [Theory]
    [InlineData("suspended")]
    [InlineData("removed")]
    [InlineData("outsider")]
    [InlineData("disabled")]
    public async Task MemberAddition_RejectsIneligibleTargets(string state)
    {
        var seed = await SeedAsync();
        var target = seed.Users[state == "outsider" ? "outsider" : "candidate"];
        await using (var db = fixture.CreateContext())
        {
            if (state is "removed" or "suspended")
                await db.OrganizationMembers.Where(m => m.OrganizationId == seed.OrganizationId && m.UserId == target)
                    .ExecuteUpdateAsync(s => s.SetProperty(m => m.Status, Enum.Parse<MembershipStatus>(state, true)));
            if (state == "disabled") await db.Users.Where(u => u.Id == target).ExecuteUpdateAsync(s => s.SetProperty(u => u.Status, UserStatus.Disabled));
        }
        using var factory = new PostgresWebApplicationFactory(fixture.ConnectionString);
        using var client = CreateClient(factory, seed.Users["owner"]);
        Assert.Equal(HttpStatusCode.NotFound, (await client.PostAsJsonAsync($"/api/v1/projects/{seed.ProjectId}/members", new { userId = target, role = "viewer" })).StatusCode);
        await using var verification = fixture.CreateContext();
        Assert.False(await verification.ProjectMembers.AnyAsync(m => m.ProjectId == seed.ProjectId && m.UserId == target));
        Assert.False(await verification.AuditLogs.AnyAsync(a => a.ProjectId == seed.ProjectId));
    }

    [Fact]
    public async Task TwoOwnersOnSeparatePages_CanLeaveButCannotRemoveRemainingOwner()
    {
        var seed = await SeedAsync();
        using var factory = new PostgresWebApplicationFactory(fixture.ConnectionString);
        using var client = CreateClient(factory, seed.Users["owner"]);
        var prefix = $"/api/v1/projects/{seed.ProjectId}";
        Assert.Equal(HttpStatusCode.OK, (await client.PatchAsJsonAsync($"{prefix}/members/{seed.Users["maintainer"]}", new { role = "projectOwner", version = 1 })).StatusCode);
        using var owners = JsonDocument.Parse(await client.GetStringAsync($"{prefix}/members?role=projectOwner&pageSize=1&page=2"));
        Assert.Single(owners.RootElement.GetProperty("data").EnumerateArray());
        Assert.Equal(2, owners.RootElement.GetProperty("meta").GetProperty("totalCount").GetInt32());
        Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync($"{prefix}/members/{seed.Users["owner"]}")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync(prefix)).StatusCode);
        using var other = CreateClient(factory, seed.Users["maintainer"]);
        Assert.Equal(HttpStatusCode.Conflict, (await other.DeleteAsync($"{prefix}/members/{seed.Users["maintainer"]}")).StatusCode);
        await using var db = fixture.CreateContext();
        Assert.Equal(MembershipStatus.Active, (await db.OrganizationMembers.SingleAsync(m => m.OrganizationId == seed.OrganizationId && m.UserId == seed.Users["owner"])).Status);
    }

    [Fact]
    public async Task ConcurrentOwnerSelfRemoval_LeavesExactlyOneActiveOwner()
    {
        var seed = await SeedAsync();
        using var factory = new PostgresWebApplicationFactory(fixture.ConnectionString);
        using var first = CreateClient(factory, seed.Users["owner"]);
        using var second = CreateClient(factory, seed.Users["maintainer"]);
        var prefix = $"/api/v1/projects/{seed.ProjectId}";
        Assert.Equal(HttpStatusCode.OK, (await first.PatchAsJsonAsync($"{prefix}/members/{seed.Users["maintainer"]}", new { role = "projectOwner", version = 1 })).StatusCode);
        var responses = await Task.WhenAll(first.DeleteAsync($"{prefix}/members/{seed.Users["owner"]}"), second.DeleteAsync($"{prefix}/members/{seed.Users["maintainer"]}"));
        Assert.Single(responses, r => r.StatusCode == HttpStatusCode.NoContent);
        Assert.Single(responses, r => r.StatusCode == HttpStatusCode.Conflict);
        await using var db = fixture.CreateContext();
        Assert.Equal(1, await db.ProjectMembers.CountAsync(m => m.ProjectId == seed.ProjectId && m.Role == ProjectRole.ProjectOwner && m.Status == MembershipStatus.Active));
        Assert.Equal(1, await db.AuditLogs.CountAsync(a => a.ProjectId == seed.ProjectId && a.EventType == "project.member_removed"));
    }

    [Theory]
    [InlineData(MembershipStatus.Removed)]
    [InlineData(MembershipStatus.Suspended)]
    public async Task RoleUpdate_InactiveMembershipReturnsStableErrorAndCanBeReactivatedExplicitly(MembershipStatus status)
    {
        var seed = await SeedAsync();
        Guid originalId;
        await using (var db = fixture.CreateContext())
        {
            var membership = await db.ProjectMembers.SingleAsync(m => m.ProjectId == seed.ProjectId && m.UserId == seed.Users["developer"]);
            originalId = membership.Id;
            if (status == MembershipStatus.Removed) membership.Remove(Now); else membership.Suspend(Now);
            await db.SaveChangesAsync();
        }
        using var factory = new PostgresWebApplicationFactory(fixture.ConnectionString);
        using var client = CreateClient(factory, seed.Users["owner"]);
        var prefix = $"/api/v1/projects/{seed.ProjectId}";
        var rejected = await client.PatchAsJsonAsync($"{prefix}/members/{seed.Users["developer"]}", new { role = "reviewer", version = 2 });
        Assert.Equal(HttpStatusCode.NotFound, rejected.StatusCode);
        using var body = JsonDocument.Parse(await rejected.Content.ReadAsStringAsync());
        Assert.Equal("MEMBERSHIP_NOT_FOUND", body.RootElement.GetProperty("error").GetProperty("code").GetString());
        Assert.Equal(HttpStatusCode.Created, (await client.PostAsJsonAsync($"{prefix}/members", new { userId = seed.Users["developer"], role = "viewer" })).StatusCode);
        await using var verification = fixture.CreateContext();
        var restored = await verification.ProjectMembers.SingleAsync(m => m.ProjectId == seed.ProjectId && m.UserId == seed.Users["developer"]);
        Assert.Equal(originalId, restored.Id); Assert.Equal(3, restored.Version); Assert.Equal(MembershipStatus.Active, restored.Status); Assert.Equal(ProjectRole.Viewer, restored.Role);
        Assert.Equal(1, await verification.AuditLogs.CountAsync(a => a.ProjectId == seed.ProjectId));
    }

    private async Task<string> GetSuffixAsync(Guid projectId)
    {
        await using var db = fixture.CreateContext();
        return (await db.Projects.SingleAsync(p => p.Id == projectId)).Slug[8..];
    }

    private async Task<Seed> SeedAsync(bool archived = false)
    {
        await using var context = fixture.CreateContext();
        var suffix = Guid.NewGuid().ToString("N");
        var users = new Dictionary<string, Guid>();
        foreach (var key in new[] { "owner", "maintainer", "developer", "candidate", "organizationAdmin", "outsider" })
        {
            var user = User.Create($"{key}-{suffix}@example.test", key, "hash", Now);
            user.MarkEmailVerified(Now);
            context.Users.Add(user);
            users.Add(key, user.Id);
        }
        var organization = Organization.Create($"Lifecycle {suffix}", $"lifecycle-{suffix}", users["owner"], Now);
        context.Organizations.Add(organization);
        foreach (var pair in users.Where(pair => pair.Key != "outsider"))
            context.OrganizationMembers.Add(OrganizationMember.Create(organization.Id, pair.Value,
                pair.Key == "owner" ? OrganizationRole.OrganizationOwner : pair.Key == "organizationAdmin"
                    ? OrganizationRole.OrganizationAdmin : OrganizationRole.OrganizationMember, users["owner"], Now));
        var project = Project.Create(organization.Id, $"Project {suffix}", $"project-{suffix}", null, "4.3", ProjectVisibility.Private, users["owner"], Now);
        if (archived) project.Archive(1, Now);
        context.Projects.Add(project);
        context.ProjectSettings.Add(ProjectSetting.Create(project.Id, Now));
        foreach (var (key, role) in new[] { ("owner", ProjectRole.ProjectOwner), ("maintainer", ProjectRole.Maintainer), ("developer", ProjectRole.Developer) })
            context.ProjectMembers.Add(ProjectMember.Create(project.Id, organization.Id, users[key], role, ProjectMemberSource.Direct, users["owner"], Now));
        await context.SaveChangesAsync();
        return new Seed(organization.Id, project.Id, users);
    }

    private sealed record Seed(Guid OrganizationId, Guid ProjectId, Dictionary<string, Guid> Users);
}
