using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using GodForge.Application.Common.Interfaces;
using GodForge.Domain.Entities.Core;
using GodForge.Domain.Entities.Identity;
using GodForge.Domain.Enums;
using GodForge.Infrastructure.Configuration;
using GodForge.IntegrationTests.Infrastructure;
using GodForge.IntegrationTests.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace GodForge.IntegrationTests.Organizations;

[Collection(PostgresPersistenceCollection.CollectionName)]
public sealed class OrganizationManagementHttpTests(PostgresPersistenceFixture fixture)
{
    private static readonly DateTimeOffset Now = new(2026, 10, 29, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task ListMembers_SupportsPaginationSearchRoleAndStatusFilters_ForAllActiveOrganizationRoles()
    {
        var seed = await SeedAsync();
        using var factory = new PostgresWebApplicationFactory(fixture.ConnectionString);

        foreach (var actorKey in new[] { "owner", "admin", "member" })
        {
            using var client = CreateClient(factory, seed.Users[actorKey]);
            var page1 = await client.GetAsync($"/api/v1/organizations/{seed.OrganizationId}/members?page=1&pageSize=2");
            Assert.Equal(HttpStatusCode.OK, page1.StatusCode);
            using var doc1 = JsonDocument.Parse(await page1.Content.ReadAsStringAsync());
            Assert.Equal(2, doc1.RootElement.GetProperty("data").GetArrayLength());
            Assert.Equal(5, doc1.RootElement.GetProperty("meta").GetProperty("totalCount").GetInt32());

            var filteredRole = await client.GetAsync($"/api/v1/organizations/{seed.OrganizationId}/members?role=organizationAdmin");
            Assert.Equal(HttpStatusCode.OK, filteredRole.StatusCode);
            using var roleDoc = JsonDocument.Parse(await filteredRole.Content.ReadAsStringAsync());
            var roleItems = roleDoc.RootElement.GetProperty("data");
            Assert.Equal(1, roleItems.GetArrayLength());
            Assert.Equal(seed.Users["admin"], roleItems[0].GetProperty("userId").GetGuid());

            var searched = await client.GetAsync($"/api/v1/organizations/{seed.OrganizationId}/members?search=secondary");
            Assert.Equal(HttpStatusCode.OK, searched.StatusCode);
            using var searchDoc = JsonDocument.Parse(await searched.Content.ReadAsStringAsync());
            var searchItems = searchDoc.RootElement.GetProperty("data");
            Assert.Equal(1, searchItems.GetArrayLength());
            Assert.Equal(seed.Users["secondaryMember"], searchItems[0].GetProperty("userId").GetGuid());

            var removedStatus = await client.GetAsync($"/api/v1/organizations/{seed.OrganizationId}/members?status=removed");
            Assert.Equal(HttpStatusCode.OK, removedStatus.StatusCode);
            using var removedDoc = JsonDocument.Parse(await removedStatus.Content.ReadAsStringAsync());
            var removedItems = removedDoc.RootElement.GetProperty("data");
            Assert.Equal(1, removedItems.GetArrayLength());
            Assert.Equal(seed.Users["removed"], removedItems[0].GetProperty("userId").GetGuid());
        }
    }

    [Theory]
    [InlineData("outsider", HttpStatusCode.NotFound)]
    [InlineData("removed", HttpStatusCode.NotFound)]
    [InlineData("suspended", HttpStatusCode.NotFound)]
    public async Task ListMembers_DeniesOutsiderRemovedAndSuspendedMember(string actorKey, HttpStatusCode expected)
    {
        var seed = await SeedAsync();
        using var factory = new PostgresWebApplicationFactory(fixture.ConnectionString);
        using var client = CreateClient(factory, seed.Users[actorKey]);

        var response = await client.GetAsync($"/api/v1/organizations/{seed.OrganizationId}/members");

        Assert.Equal(expected, response.StatusCode);
    }

    [Fact]
    public async Task AuthorizationMatrix_AdminCannotModifyOwnerOrAdmin_AndMemberCannotInviteOrModify()
    {
        var seed = await SeedAsync();
        using var factory = new PostgresWebApplicationFactory(fixture.ConnectionString);
        using var adminClient = CreateClient(factory, seed.Users["admin"]);
        using var memberClient = CreateClient(factory, seed.Users["member"]);
        using var outsiderClient = CreateClient(factory, seed.Users["outsider"]);

        // 1. Admin attempting to modify Owner -> 403 Forbidden
        var adminModifiesOwner = await adminClient.PatchAsJsonAsync(
            $"/api/v1/organizations/{seed.OrganizationId}/members/{seed.Users["owner"]}",
            new { role = "organizationMember", status = "active", version = 1 });
        Assert.Equal(HttpStatusCode.Forbidden, adminModifiesOwner.StatusCode);

        // Admin attempting to remove Owner -> 403 Forbidden
        var adminRemovesOwner = await adminClient.DeleteAsync(
            $"/api/v1/organizations/{seed.OrganizationId}/members/{seed.Users["owner"]}");
        Assert.Equal(HttpStatusCode.Forbidden, adminRemovesOwner.StatusCode);

        // Admin attempting to promote Member to Admin -> 403 Forbidden
        var adminPromotesToAdmin = await adminClient.PatchAsJsonAsync(
            $"/api/v1/organizations/{seed.OrganizationId}/members/{seed.Users["member"]}",
            new { role = "organizationAdmin", status = "active", version = 1 });
        Assert.Equal(HttpStatusCode.Forbidden, adminPromotesToAdmin.StatusCode);

        // 2. Member attempting to invite users -> 403 Forbidden
        var memberInvites = await memberClient.PostAsJsonAsync(
            $"/api/v1/organizations/{seed.OrganizationId}/invitations",
            new { email = "blocked-invite@example.test", role = "organizationMember" });
        Assert.Equal(HttpStatusCode.Forbidden, memberInvites.StatusCode);

        // Member attempting to list invitations -> 403 Forbidden
        var memberListsInvites = await memberClient.GetAsync(
            $"/api/v1/organizations/{seed.OrganizationId}/invitations");
        Assert.Equal(HttpStatusCode.Forbidden, memberListsInvites.StatusCode);

        // 3. Outsider attempting to list invitations -> 404 NotFound
        var outsiderListsInvites = await outsiderClient.GetAsync(
            $"/api/v1/organizations/{seed.OrganizationId}/invitations");
        Assert.Equal(HttpStatusCode.NotFound, outsiderListsInvites.StatusCode);

        // 4. Outsider attempting to transfer ownership -> 404 NotFound
        var outsiderTransfers = await outsiderClient.PostAsJsonAsync(
            $"/api/v1/organizations/{seed.OrganizationId}/transfer-ownership",
            new { newOwnerUserId = seed.Users["admin"], retainCurrentOwnerAs = "organizationAdmin", version = 1 });
        Assert.Equal(HttpStatusCode.NotFound, outsiderTransfers.StatusCode);

        // 5. Cross-organization member ID manipulation -> 404 MEMBERSHIP_NOT_FOUND
        using var ownerClient = CreateClient(factory, seed.Users["owner"]);
        var crossOrgPatch = await ownerClient.PatchAsJsonAsync(
            $"/api/v1/organizations/{seed.OrganizationId}/members/{seed.Users["otherOrgMember"]}",
            new { role = "organizationAdmin", status = "active", version = 1 });
        Assert.Equal(HttpStatusCode.NotFound, crossOrgPatch.StatusCode);
        Assert.Equal("MEMBERSHIP_NOT_FOUND", await ReadErrorCodeAsync(crossOrgPatch));
    }

    [Fact]
    public async Task RemoveMember_RevokesAllProjectMembershipsInSameTransaction_AndImmediatelyDeniesAccess()
    {
        var seed = await SeedAsync();
        using var factory = new PostgresWebApplicationFactory(fixture.ConnectionString);
        using var ownerClient = CreateClient(factory, seed.Users["owner"]);
        using var memberClient = CreateClient(factory, seed.Users["member"]);

        // Verify member can access organization and both projects before removal
        Assert.Equal(HttpStatusCode.OK, (await memberClient.GetAsync($"/api/v1/organizations/{seed.OrganizationId}")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await memberClient.GetAsync($"/api/v1/projects/{seed.ProjectOneId}")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await memberClient.GetAsync($"/api/v1/projects/{seed.ProjectTwoId}")).StatusCode);

        // Remove member from organization
        var removeResponse = await ownerClient.DeleteAsync(
            $"/api/v1/organizations/{seed.OrganizationId}/members/{seed.Users["member"]}");
        Assert.Equal(HttpStatusCode.NoContent, removeResponse.StatusCode);

        // Verify member immediately loses access to organization and both projects
        Assert.Equal(HttpStatusCode.NotFound, (await memberClient.GetAsync($"/api/v1/organizations/{seed.OrganizationId}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await memberClient.GetAsync($"/api/v1/projects/{seed.ProjectOneId}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await memberClient.GetAsync($"/api/v1/projects/{seed.ProjectTwoId}")).StatusCode);

        await using var context = fixture.CreateContext();
        var orgMembership = await context.OrganizationMembers.SingleAsync(
            m => m.OrganizationId == seed.OrganizationId && m.UserId == seed.Users["member"]);
        Assert.Equal(MembershipStatus.Removed, orgMembership.Status);

        var projectMemberships = await context.ProjectMembers
            .Where(m => m.OrganizationId == seed.OrganizationId && m.UserId == seed.Users["member"])
            .ToListAsync();
        Assert.Equal(2, projectMemberships.Count);
        Assert.All(projectMemberships, m => Assert.Equal(MembershipStatus.Removed, m.Status));
        Assert.True(await context.AuditLogs.AnyAsync(
            a => a.ResourceId == orgMembership.Id && a.EventType == "organization.member_removed"));
    }

    [Fact]
    public async Task LastOwnerProtection_PreventsRemovingLastOwner_AndAllowsMemberSelfRemoval()
    {
        var seed = await SeedAsync();
        using var factory = new PostgresWebApplicationFactory(fixture.ConnectionString);
        using var ownerClient = CreateClient(factory, seed.Users["owner"]);
        using var secondaryClient = CreateClient(factory, seed.Users["secondaryMember"]);

        // Last owner cannot remove themselves
        var ownerSelfRemove = await ownerClient.DeleteAsync(
            $"/api/v1/organizations/{seed.OrganizationId}/members/{seed.Users["owner"]}");
        Assert.Equal(HttpStatusCode.Conflict, ownerSelfRemove.StatusCode);
        Assert.Equal("LAST_OWNER_REQUIRED", await ReadErrorCodeAsync(ownerSelfRemove));

        // Regular member can self-remove (leave organization)
        var memberSelfRemove = await secondaryClient.DeleteAsync(
            $"/api/v1/organizations/{seed.OrganizationId}/members/{seed.Users["secondaryMember"]}");
        Assert.Equal(HttpStatusCode.NoContent, memberSelfRemove.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await secondaryClient.GetAsync($"/api/v1/organizations/{seed.OrganizationId}")).StatusCode);
    }

    [Fact]
    public async Task InvitationSecurity_FullLifecycle_Replacement_Idempotency_WrongEmail_Revoked_Expired_Reused_ExistingMember_AndNoRawTokenLeak()
    {
        var seed = await SeedAsync();
        using var factory = new PostgresWebApplicationFactory(fixture.ConnectionString);
        using var ownerClient = CreateClient(factory, seed.Users["owner"]);
        using var inviteeClient = CreateClient(factory, seed.Users["outsider"]);
        using var wrongUserClient = CreateClient(factory, seed.Users["otherOrgMember"]);
        var encryptionKey = factory.Services.GetRequiredService<IOptions<OutboxEncryptionSettings>>().Value.Key;

        var inviteeEmail = seed.Emails["outsider"];

        // 1 & 7. Create invitation with Idempotency-Key and replay same key + same payload
        using var createReq1 = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/organizations/{seed.OrganizationId}/invitations")
        {
            Content = JsonContent.Create(new { email = inviteeEmail, role = "organizationMember" })
        };
        createReq1.Headers.Add("Idempotency-Key", "invite-idem-001");
        var createRes1 = await ownerClient.SendAsync(createReq1);
        Assert.Equal(HttpStatusCode.Created, createRes1.StatusCode);
        var createBody1 = await createRes1.Content.ReadAsStringAsync();
        using var createDoc1 = JsonDocument.Parse(createBody1);
        var invitationId = createDoc1.RootElement.GetProperty("data").GetProperty("id").GetGuid();

        // Replay with same Idempotency-Key and same payload returns same invitation without sending a second email
        using var createReqReplay = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/organizations/{seed.OrganizationId}/invitations")
        {
            Content = JsonContent.Create(new { email = inviteeEmail, role = "organizationMember" })
        };
        createReqReplay.Headers.Add("Idempotency-Key", "invite-idem-001");
        var createResReplay = await ownerClient.SendAsync(createReqReplay);
        Assert.Equal(HttpStatusCode.Created, createResReplay.StatusCode);
        using var replayDoc = JsonDocument.Parse(await createResReplay.Content.ReadAsStringAsync());
        Assert.Equal(invitationId, replayDoc.RootElement.GetProperty("data").GetProperty("id").GetGuid());

        // Replay with same Idempotency-Key and different payload returns 409 IDEMPOTENCY_KEY_REUSED
        using var createReqConflict = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/organizations/{seed.OrganizationId}/invitations")
        {
            Content = JsonContent.Create(new { email = inviteeEmail, role = "organizationAdmin" })
        };
        createReqConflict.Headers.Add("Idempotency-Key", "invite-idem-001");
        var createResConflict = await ownerClient.SendAsync(createReqConflict);
        Assert.Equal(HttpStatusCode.Conflict, createResConflict.StatusCode);
        Assert.Equal("IDEMPOTENCY_KEY_REUSED", await ReadErrorCodeAsync(createResConflict));

        string firstToken;
        await using (var context = fixture.CreateContext())
        {
            var outboxMessages = await context.OutboxMessages
                .OrderBy(m => m.CreatedAt)
                .ToListAsync();
            var latestOutbox = outboxMessages.Last();

            // 10. Verify raw token is encrypted at rest in Outbox and never present in API response or AuditLogs
            Assert.Contains("ciphertext", latestOutbox.PayloadJson, StringComparison.Ordinal);
            firstToken = ExtractTokenFromEncryptedOutbox(encryptionKey, latestOutbox.PayloadJson);
            Assert.DoesNotContain(firstToken, createBody1, StringComparison.Ordinal);
            Assert.DoesNotContain(firstToken, latestOutbox.PayloadJson, StringComparison.Ordinal);
            Assert.DoesNotContain(inviteeEmail, latestOutbox.PayloadJson, StringComparison.OrdinalIgnoreCase);

            var auditLogs = await context.AuditLogs
                .Where(a => a.ResourceId == invitationId)
                .ToListAsync();
            Assert.NotEmpty(auditLogs);
            Assert.All(auditLogs, log => Assert.DoesNotContain(firstToken, log.DetailsJson ?? string.Empty, StringComparison.Ordinal));
        }

        // 9. Pending invitation replacement: sending a new invitation without the old Idempotency-Key replaces the token
        var replaceRes = await ownerClient.PostAsJsonAsync(
            $"/api/v1/organizations/{seed.OrganizationId}/invitations",
            new { email = inviteeEmail, role = "organizationAdmin" });
        Assert.Equal(HttpStatusCode.Created, replaceRes.StatusCode);

        string replacementToken;
        await using (var context = fixture.CreateContext())
        {
            var latestOutbox = await context.OutboxMessages.OrderBy(m => m.CreatedAt).LastAsync();
            replacementToken = ExtractTokenFromEncryptedOutbox(encryptionKey, latestOutbox.PayloadJson);
        }
        Assert.NotEqual(firstToken, replacementToken);

        // Old replaced token is rejected
        var oldTokenAccept = await inviteeClient.PostAsJsonAsync(
            "/api/v1/organization-invitations/accept",
            new { token = firstToken });
        Assert.Equal(HttpStatusCode.Unauthorized, oldTokenAccept.StatusCode);
        Assert.Equal("INVITE_INVALID_OR_EXPIRED", await ReadErrorCodeAsync(oldTokenAccept));

        // 2. Wrong user email attempting acceptance -> 401 INVITE_INVALID_OR_EXPIRED
        var wrongUserAccept = await wrongUserClient.PostAsJsonAsync(
            "/api/v1/organization-invitations/accept",
            new { token = replacementToken });
        Assert.Equal(HttpStatusCode.Unauthorized, wrongUserAccept.StatusCode);
        Assert.Equal("INVITE_INVALID_OR_EXPIRED", await ReadErrorCodeAsync(wrongUserAccept));

        // 1. Valid invitation acceptance with replacement token
        var validAccept = await inviteeClient.PostAsJsonAsync(
            "/api/v1/organization-invitations/accept",
            new { token = replacementToken });
        Assert.Equal(HttpStatusCode.OK, validAccept.StatusCode);
        using var acceptDoc = JsonDocument.Parse(await validAccept.Content.ReadAsStringAsync());
        Assert.Equal(seed.OrganizationId, acceptDoc.RootElement.GetProperty("data").GetProperty("organization").GetProperty("id").GetGuid());
        Assert.Equal("organizationAdmin", acceptDoc.RootElement.GetProperty("data").GetProperty("membership").GetProperty("role").GetString());

        // 5. Reused invitation token -> 401 INVITE_INVALID_OR_EXPIRED
        var reusedAccept = await inviteeClient.PostAsJsonAsync(
            "/api/v1/organization-invitations/accept",
            new { token = replacementToken });
        Assert.Equal(HttpStatusCode.Unauthorized, reusedAccept.StatusCode);
        Assert.Equal("INVITE_INVALID_OR_EXPIRED", await ReadErrorCodeAsync(reusedAccept));

        // 4. Revoked invitation token -> 401 INVITE_INVALID_OR_EXPIRED
        var revokeCreate = await ownerClient.PostAsJsonAsync(
            $"/api/v1/organizations/{seed.OrganizationId}/invitations",
            new { email = seed.Emails["otherOrgMember"], role = "organizationMember" });
        Assert.Equal(HttpStatusCode.Created, revokeCreate.StatusCode);
        using var revokeCreateDoc = JsonDocument.Parse(await revokeCreate.Content.ReadAsStringAsync());
        var revokedInviteId = revokeCreateDoc.RootElement.GetProperty("data").GetProperty("id").GetGuid();

        string revokedToken;
        await using (var context = fixture.CreateContext())
        {
            var latestOutbox = await context.OutboxMessages.OrderBy(m => m.CreatedAt).LastAsync();
            revokedToken = ExtractTokenFromEncryptedOutbox(encryptionKey, latestOutbox.PayloadJson);
        }

        var revokeRes = await ownerClient.DeleteAsync(
            $"/api/v1/organizations/{seed.OrganizationId}/invitations/{revokedInviteId}");
        Assert.Equal(HttpStatusCode.NoContent, revokeRes.StatusCode);

        var revokedAccept = await wrongUserClient.PostAsJsonAsync(
            "/api/v1/organization-invitations/accept",
            new { token = revokedToken });
        Assert.Equal(HttpStatusCode.Unauthorized, revokedAccept.StatusCode);
        Assert.Equal("INVITE_INVALID_OR_EXPIRED", await ReadErrorCodeAsync(revokedAccept));

        // 3. Expired invitation token -> 401 INVITE_INVALID_OR_EXPIRED
        var secretHash = factory.Services.GetRequiredService<ISecretHashService>();
        const string expiredRawToken = "expired-raw-token-for-integration-test";
        await using (var context = fixture.CreateContext())
        {
            var expiredInvite = UserInvite.Create(
                seed.OrganizationId,
                seed.Emails["otherOrgMember"],
                OrganizationRole.OrganizationMember,
                secretHash.Hash(expiredRawToken),
                seed.Users["owner"],
                DateTimeOffset.UtcNow.AddMinutes(-5),
                DateTimeOffset.UtcNow.AddDays(-7));
            context.UserInvites.Add(expiredInvite);
            await context.SaveChangesAsync();
        }

        var expiredAccept = await wrongUserClient.PostAsJsonAsync(
            "/api/v1/organization-invitations/accept",
            new { token = expiredRawToken });
        Assert.Equal(HttpStatusCode.Unauthorized, expiredAccept.StatusCode);
        Assert.Equal("INVITE_INVALID_OR_EXPIRED", await ReadErrorCodeAsync(expiredAccept));

        // 8. Invitation for a user who already has an active membership does not duplicate membership or downgrade role
        var existingMemberInviteRes = await ownerClient.PostAsJsonAsync(
            $"/api/v1/organizations/{seed.OrganizationId}/invitations",
            new { email = inviteeEmail, role = "organizationMember" });
        Assert.Equal(HttpStatusCode.Created, existingMemberInviteRes.StatusCode);

        string existingMemberToken;
        await using (var context = fixture.CreateContext())
        {
            var latestOutbox = await context.OutboxMessages.OrderBy(m => m.CreatedAt).LastAsync();
            existingMemberToken = ExtractTokenFromEncryptedOutbox(encryptionKey, latestOutbox.PayloadJson);
        }

        var existingMemberAccept = await inviteeClient.PostAsJsonAsync(
            "/api/v1/organization-invitations/accept",
            new { token = existingMemberToken });
        Assert.Equal(HttpStatusCode.OK, existingMemberAccept.StatusCode);
        using var existingAcceptDoc = JsonDocument.Parse(await existingMemberAccept.Content.ReadAsStringAsync());
        Assert.Equal("organizationAdmin", existingAcceptDoc.RootElement.GetProperty("data").GetProperty("membership").GetProperty("role").GetString());
    }

    [Fact]
    public async Task Concurrency_SimultaneousInvitationAcceptance_AllowsOnlyOneSuccessAndCreatesSingleMembership()
    {
        var seed = await SeedAsync();
        using var factory = new PostgresWebApplicationFactory(fixture.ConnectionString);
        using var ownerClient = CreateClient(factory, seed.Users["owner"]);
        using var inviteeClient1 = CreateClient(factory, seed.Users["outsider"]);
        using var inviteeClient2 = CreateClient(factory, seed.Users["outsider"]);
        var encryptionKey = factory.Services.GetRequiredService<IOptions<OutboxEncryptionSettings>>().Value.Key;

        var createRes = await ownerClient.PostAsJsonAsync(
            $"/api/v1/organizations/{seed.OrganizationId}/invitations",
            new { email = seed.Emails["outsider"], role = "organizationMember" });
        Assert.Equal(HttpStatusCode.Created, createRes.StatusCode);

        string rawToken;
        await using (var context = fixture.CreateContext())
        {
            var latestOutbox = await context.OutboxMessages.OrderBy(m => m.CreatedAt).LastAsync();
            rawToken = ExtractTokenFromEncryptedOutbox(encryptionKey, latestOutbox.PayloadJson);
        }

        var responses = await Task.WhenAll(
            inviteeClient1.PostAsJsonAsync("/api/v1/organization-invitations/accept", new { token = rawToken }),
            inviteeClient2.PostAsJsonAsync("/api/v1/organization-invitations/accept", new { token = rawToken }));

        Assert.Single(responses, r => r.StatusCode == HttpStatusCode.OK);
        var failed = Assert.Single(responses, r => r.StatusCode != HttpStatusCode.OK);
        Assert.Equal(HttpStatusCode.Unauthorized, failed.StatusCode);
        Assert.Equal("INVITE_INVALID_OR_EXPIRED", await ReadErrorCodeAsync(failed));

        await using var verifyContext = fixture.CreateContext();
        Assert.Equal(1, await verifyContext.OrganizationMembers.CountAsync(
            m => m.OrganizationId == seed.OrganizationId && m.UserId == seed.Users["outsider"] && m.Status == MembershipStatus.Active));
    }

    [Fact]
    public async Task Concurrency_RoleUpdates_LastOwnerProtection_AndRemovalRacingWithRoleUpdate()
    {
        var seed = await SeedAsync();
        using var factory = new PostgresWebApplicationFactory(fixture.ConnectionString);
        using var ownerClient1 = CreateClient(factory, seed.Users["owner"]);
        using var ownerClient2 = CreateClient(factory, seed.Users["owner"]);

        // 1. Two concurrent role updates on the same membership with version 1
        var roleUpdateResponses = await Task.WhenAll(
            ownerClient1.PatchAsJsonAsync(
                $"/api/v1/organizations/{seed.OrganizationId}/members/{seed.Users["member"]}",
                new { role = "organizationAdmin", status = "active", version = 1 }),
            ownerClient2.PatchAsJsonAsync(
                $"/api/v1/organizations/{seed.OrganizationId}/members/{seed.Users["member"]}",
                new { role = "organizationMember", status = "suspended", version = 1 }));

        Assert.Single(roleUpdateResponses, r => r.StatusCode == HttpStatusCode.OK);
        var conflictResponse = Assert.Single(roleUpdateResponses, r => r.StatusCode != HttpStatusCode.OK);
        Assert.Equal(HttpStatusCode.Conflict, conflictResponse.StatusCode);
        Assert.Equal("CONCURRENCY_CONFLICT", await ReadErrorCodeAsync(conflictResponse));

        // 5. Last-owner protection under concurrent requests: promote admin to second owner, then both owners concurrently try to demote each other
        await using (var context = fixture.CreateContext())
        {
            var adminMember = await context.OrganizationMembers.SingleAsync(
                m => m.OrganizationId == seed.OrganizationId && m.UserId == seed.Users["admin"]);
            adminMember.Change(OrganizationRole.OrganizationOwner, MembershipStatus.Active, seed.Users["owner"], adminMember.Version, Now);
            await context.SaveChangesAsync();
        }

        using var secondOwnerClient = CreateClient(factory, seed.Users["admin"]);
        var demoteResponses = await Task.WhenAll(
            ownerClient1.PatchAsJsonAsync(
                $"/api/v1/organizations/{seed.OrganizationId}/members/{seed.Users["owner"]}",
                new { role = "organizationAdmin", status = "active", version = 1 }),
            secondOwnerClient.PatchAsJsonAsync(
                $"/api/v1/organizations/{seed.OrganizationId}/members/{seed.Users["admin"]}",
                new { role = "organizationAdmin", status = "active", version = 2 }));

        Assert.Single(demoteResponses, r => r.StatusCode == HttpStatusCode.OK);
        var lastOwnerBlocked = Assert.Single(demoteResponses, r => r.StatusCode != HttpStatusCode.OK);
        Assert.Equal(HttpStatusCode.Conflict, lastOwnerBlocked.StatusCode);
        Assert.Equal("LAST_OWNER_REQUIRED", await ReadErrorCodeAsync(lastOwnerBlocked));

        await using (var verifyContext = fixture.CreateContext())
        {
            var activeOwners = await verifyContext.OrganizationMembers.CountAsync(
                m => m.OrganizationId == seed.OrganizationId &&
                     m.Role == OrganizationRole.OrganizationOwner &&
                     m.Status == MembershipStatus.Active);
            Assert.Equal(1, activeOwners);
        }

        // 4. Membership removal racing with role update on secondaryMember
        await using var activeOwnerContext = fixture.CreateContext();
        var currentOwner = await activeOwnerContext.OrganizationMembers.SingleAsync(
            m => m.OrganizationId == seed.OrganizationId &&
                 m.Role == OrganizationRole.OrganizationOwner &&
                 m.Status == MembershipStatus.Active);
        using var activeOwnerClient1 = CreateClient(factory, currentOwner.UserId);
        using var activeOwnerClient2 = CreateClient(factory, currentOwner.UserId);

        var raceResponses = await Task.WhenAll(
            activeOwnerClient1.DeleteAsync($"/api/v1/organizations/{seed.OrganizationId}/members/{seed.Users["secondaryMember"]}"),
            activeOwnerClient2.PatchAsJsonAsync(
                $"/api/v1/organizations/{seed.OrganizationId}/members/{seed.Users["secondaryMember"]}",
                new { role = "organizationAdmin", status = "active", version = 1 }));

        Assert.Contains(raceResponses, r => r.StatusCode == HttpStatusCode.NoContent);
        await using var finalContext = fixture.CreateContext();
        var removedTarget = await finalContext.OrganizationMembers.SingleAsync(
            m => m.OrganizationId == seed.OrganizationId && m.UserId == seed.Users["secondaryMember"]);
        Assert.Equal(MembershipStatus.Removed, removedTarget.Status);
    }

    [Fact]
    public async Task OrganizationSettingsAndDeletion_EnforceOptimisticConcurrency_SlugRules_AndDeletingLifecycleState()
    {
        var seed = await SeedAsync();
        using var factory = new PostgresWebApplicationFactory(fixture.ConnectionString);
        using var ownerClient = CreateClient(factory, seed.Users["owner"]);
        using var adminClient = CreateClient(factory, seed.Users["admin"]);

        // Admin can update general settings (name and slug)
        var updateRes = await adminClient.PatchAsJsonAsync(
            $"/api/v1/organizations/{seed.OrganizationId}",
            new { name = "Updated Studio Name", slug = $"updated-{seed.Suffix}", version = 1 });
        Assert.Equal(HttpStatusCode.OK, updateRes.StatusCode);
        using var updateDoc = JsonDocument.Parse(await updateRes.Content.ReadAsStringAsync());
        Assert.Equal("Updated Studio Name", updateDoc.RootElement.GetProperty("data").GetProperty("name").GetString());
        Assert.Equal($"updated-{seed.Suffix}", updateDoc.RootElement.GetProperty("data").GetProperty("slug").GetString());
        Assert.Equal(2, updateDoc.RootElement.GetProperty("data").GetProperty("version").GetInt32());

        // Stale version update fails with 409 CONCURRENCY_CONFLICT
        var staleUpdate = await ownerClient.PatchAsJsonAsync(
            $"/api/v1/organizations/{seed.OrganizationId}",
            new { name = "Stale Attempt", slug = $"stale-{seed.Suffix}", version = 1 });
        Assert.Equal(HttpStatusCode.Conflict, staleUpdate.StatusCode);
        Assert.Equal("CONCURRENCY_CONFLICT", await ReadErrorCodeAsync(staleUpdate));

        // Admin cannot request organization deletion -> 403 Forbidden
        var adminDelete = await SendDeleteJsonAsync(
            adminClient,
            $"/api/v1/organizations/{seed.OrganizationId}",
            new { version = 2, confirmationSlug = $"updated-{seed.Suffix}" });
        Assert.Equal(HttpStatusCode.Forbidden, adminDelete.StatusCode);

        // Owner with wrong confirmationSlug -> 400 VALIDATION_ERROR
        var wrongSlugDelete = await SendDeleteJsonAsync(
            ownerClient,
            $"/api/v1/organizations/{seed.OrganizationId}",
            new { version = 2, confirmationSlug = "wrong-slug" });
        Assert.Equal(HttpStatusCode.BadRequest, wrongSlugDelete.StatusCode);
        Assert.Equal("VALIDATION_ERROR", await ReadErrorCodeAsync(wrongSlugDelete));

        // Owner with valid confirmationSlug and version -> 202 Accepted with status = "deleting"
        var validDelete = await SendDeleteJsonAsync(
            ownerClient,
            $"/api/v1/organizations/{seed.OrganizationId}",
            new { version = 2, confirmationSlug = $"updated-{seed.Suffix}" });
        Assert.Equal(HttpStatusCode.Accepted, validDelete.StatusCode);
        using var deleteDoc = JsonDocument.Parse(await validDelete.Content.ReadAsStringAsync());
        Assert.Equal("deleting", deleteDoc.RootElement.GetProperty("data").GetProperty("status").GetString());
        Assert.Equal(3, deleteDoc.RootElement.GetProperty("data").GetProperty("version").GetInt32());

        // Mutations on a deleting organization return 409 ORGANIZATION_NOT_ACTIVE
        var mutateWhileDeleting = await ownerClient.PatchAsJsonAsync(
            $"/api/v1/organizations/{seed.OrganizationId}",
            new { name = "Blocked", slug = $"blocked-{seed.Suffix}", version = 3 });
        Assert.Equal(HttpStatusCode.Conflict, mutateWhileDeleting.StatusCode);
        Assert.Equal("ORGANIZATION_NOT_ACTIVE", await ReadErrorCodeAsync(mutateWhileDeleting));
    }

    [Fact]
    public async Task OrganizationValidationBoundaries_AcceptsMinAndMaxNameAndSlug_AndRejectsInvalidBoundaries()
    {
        var seed = await SeedAsync();
        using var factory = new PostgresWebApplicationFactory(fixture.ConnectionString);
        using var ownerClient = CreateClient(factory, seed.Users["owner"]);

        // 1. Minimum valid name length (1 char) and valid short slug
        var minSlug = $"m-{seed.Suffix}";
        var minUpdate = await ownerClient.PatchAsJsonAsync(
            $"/api/v1/organizations/{seed.OrganizationId}",
            new { name = "X", slug = minSlug, version = 1 });
        Assert.Equal(HttpStatusCode.OK, minUpdate.StatusCode);

        // 2. Maximum valid name length (160 chars) and maximum valid slug length (80 chars)
        var maxName = new string('G', Organization.MaxNameLength);
        var slugPrefix = $"max-{seed.Suffix}-";
        var maxSlug = slugPrefix + new string('z', Organization.MaxSlugLength - slugPrefix.Length);
        Assert.Equal(80, maxSlug.Length);

        var maxUpdate = await ownerClient.PatchAsJsonAsync(
            $"/api/v1/organizations/{seed.OrganizationId}",
            new { name = maxName, slug = maxSlug, version = 2 });
        Assert.Equal(HttpStatusCode.OK, maxUpdate.StatusCode);

        var getRes = await ownerClient.GetAsync($"/api/v1/organizations/{seed.OrganizationId}");
        Assert.Equal(HttpStatusCode.OK, getRes.StatusCode);
        using var getDoc = JsonDocument.Parse(await getRes.Content.ReadAsStringAsync());
        Assert.Equal(maxName, getDoc.RootElement.GetProperty("data").GetProperty("name").GetString());
        Assert.Equal(maxSlug, getDoc.RootElement.GetProperty("data").GetProperty("slug").GetString());

        // 3. Reject >160 char name, >80 char slug, whitespace name, leading/trailing/consecutive hyphens
        foreach (var invalidPayload in new object[]
                 {
                     new { name = new string('G', 161), slug = maxSlug, version = 3 },
                     new { name = "Valid", slug = maxSlug + "a", version = 3 },
                     new { name = "   ", slug = maxSlug, version = 3 },
                     new { name = "Valid", slug = "-leading", version = 3 },
                     new { name = "Valid", slug = "trailing-", version = 3 },
                     new { name = "Valid", slug = "double--hyphen", version = 3 },
                     new { name = "Valid", slug = "UpperCase", version = 3 }
                 })
        {
            var badRes = await ownerClient.PatchAsJsonAsync(
                $"/api/v1/organizations/{seed.OrganizationId}",
                invalidPayload);
            Assert.Equal(HttpStatusCode.BadRequest, badRes.StatusCode);
            Assert.Equal("VALIDATION_ERROR", await ReadErrorCodeAsync(badRes));
        }
    }

    [Fact]
    public async Task MultiOwnerAndLargeOrganization_SupportsCrossPageOwnerRemoval_AndBeyond100MemberOwnershipTransfer()
    {
        var seed = await SeedAsync();
        await using (var context = fixture.CreateContext())
        {
            // Promote admin to a second active OrganizationOwner
            var adminMember = await context.OrganizationMembers.SingleAsync(
                m => m.OrganizationId == seed.OrganizationId && m.UserId == seed.Users["admin"]);
            adminMember.Change(OrganizationRole.OrganizationOwner, MembershipStatus.Active, seed.Users["owner"], adminMember.Version, Now);

            // Add 102 active members so total active members > 105 (spanning 6 pages at pageSize=20)
            for (var i = 1; i <= 102; i++)
            {
                var bulkUser = User.Create(
                    $"bulk-{i:D3}-{seed.Suffix}@example.test",
                    $"Bulk Candidate {i:D3}",
                    "hash",
                    Now.AddMinutes(i));
                bulkUser.MarkEmailVerified(Now.AddMinutes(i));
                context.Users.Add(bulkUser);
                context.OrganizationMembers.Add(
                    OrganizationMember.Create(
                        seed.OrganizationId,
                        bulkUser.Id,
                        OrganizationRole.OrganizationMember,
                        seed.Users["owner"],
                        Now.AddMinutes(i)));
            }

            await context.SaveChangesAsync();
        }

        using var factory = new PostgresWebApplicationFactory(fixture.ConnectionString);
        using var ownerClient = CreateClient(factory, seed.Users["owner"]);
        using var secondOwnerClient = CreateClient(factory, seed.Users["admin"]);

        // 1. Search for candidate #102 (beyond the first 100 members) and also locate via page 6 (pageSize=20)
        var page6Res = await ownerClient.GetAsync(
            $"/api/v1/organizations/{seed.OrganizationId}/members?page=6&pageSize=20&status=active");
        Assert.Equal(HttpStatusCode.OK, page6Res.StatusCode);
        using var page6Doc = JsonDocument.Parse(await page6Res.Content.ReadAsStringAsync());
        Assert.True(page6Doc.RootElement.GetProperty("meta").GetProperty("totalCount").GetInt32() > 100);
        Assert.NotEmpty(page6Doc.RootElement.GetProperty("data").EnumerateArray());

        var searchRes = await ownerClient.GetAsync(
            $"/api/v1/organizations/{seed.OrganizationId}/members?page=1&pageSize=20&status=active&search=Bulk%20Candidate%20102");
        Assert.Equal(HttpStatusCode.OK, searchRes.StatusCode);
        using var searchDoc = JsonDocument.Parse(await searchRes.Content.ReadAsStringAsync());
        var candidate102Id = searchDoc.RootElement.GetProperty("data")[0].GetProperty("userId").GetGuid();

        // 2. With two active owners, secondOwner can self-remove (or be demoted) even if a filtered search only shows one owner
        var removeSecondOwner = await secondOwnerClient.DeleteAsync(
            $"/api/v1/organizations/{seed.OrganizationId}/members/{seed.Users["admin"]}");
        Assert.Equal(HttpStatusCode.NoContent, removeSecondOwner.StatusCode);

        // 3. Remaining sole owner cannot be removed or demoted
        var removeLastOwner = await ownerClient.DeleteAsync(
            $"/api/v1/organizations/{seed.OrganizationId}/members/{seed.Users["owner"]}");
        Assert.Equal(HttpStatusCode.Conflict, removeLastOwner.StatusCode);
        Assert.Equal("LAST_OWNER_REQUIRED", await ReadErrorCodeAsync(removeLastOwner));

        // 4. Transfer ownership to candidate #102 (located beyond the first 100 members) and verify concurrency conflict on stale version
        var transferResponses = await Task.WhenAll(
            ownerClient.PostAsJsonAsync(
                $"/api/v1/organizations/{seed.OrganizationId}/transfer-ownership",
                new { newOwnerUserId = candidate102Id, retainCurrentOwnerAs = "organizationAdmin", version = 1 }),
            ownerClient.PostAsJsonAsync(
                $"/api/v1/organizations/{seed.OrganizationId}/transfer-ownership",
                new { newOwnerUserId = seed.Users["member"], retainCurrentOwnerAs = "organizationAdmin", version = 1 }));

        Assert.Single(transferResponses, r => r.StatusCode == HttpStatusCode.OK);
        var failedTransfer = Assert.Single(transferResponses, r => r.StatusCode != HttpStatusCode.OK);
        Assert.True(
            failedTransfer.StatusCode is HttpStatusCode.Conflict or HttpStatusCode.Forbidden,
            $"Expected Conflict or Forbidden for losing concurrent transfer, got {failedTransfer.StatusCode}");
    }

    private static string ExtractTokenFromEncryptedOutbox(string encryptionKey, string encryptedPayload)
    {
        using var envelopeDoc = JsonDocument.Parse(encryptedPayload);
        var nonce = Convert.FromBase64String(envelopeDoc.RootElement.GetProperty("nonce").GetString()!);
        var ciphertext = Convert.FromBase64String(envelopeDoc.RootElement.GetProperty("ciphertext").GetString()!);
        var tag = Convert.FromBase64String(envelopeDoc.RootElement.GetProperty("tag").GetString()!);
        var plaintext = new byte[ciphertext.Length];
        var key = SHA256.HashData(Encoding.UTF8.GetBytes(encryptionKey));
        using (var aes = new AesGcm(key, tag.Length))
            aes.Decrypt(nonce, ciphertext, tag, plaintext);

        using var doc = JsonDocument.Parse(plaintext);
        var htmlBody = doc.RootElement.GetProperty("htmlBody").GetString() ?? string.Empty;
        var match = Regex.Match(htmlBody, @"token=([^""&]+)");
        Assert.True(match.Success, "Expected invitation URL with token query parameter in outbox HTML body.");
        return Uri.UnescapeDataString(match.Groups[1].Value);
    }

    private static async Task<HttpResponseMessage> SendDeleteJsonAsync(HttpClient client, string url, object body)
    {
        using var request = new HttpRequestMessage(HttpMethod.Delete, url)
        {
            Content = JsonContent.Create(body)
        };
        return await client.SendAsync(request);
    }

    private static async Task<string?> ReadErrorCodeAsync(HttpResponseMessage response)
    {
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return doc.RootElement.GetProperty("error").GetProperty("code").GetString();
    }

    private static HttpClient CreateClient(PostgresWebApplicationFactory factory, Guid actorId)
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthHandler.UserIdHeader, actorId.ToString());
        return client;
    }

    private async Task<SeedData> SeedAsync()
    {
        await using var context = fixture.CreateContext();
        var suffix = Guid.NewGuid().ToString("N")[..12];
        var users = new Dictionary<string, Guid>();
        var emails = new Dictionary<string, string>();

        foreach (var (key, displayName) in new[]
                 {
                     ("owner", "Owner Alpha"),
                     ("admin", "Admin Bravo"),
                     ("member", "Member Charlie"),
                     ("secondaryMember", "Secondary Delta"),
                     ("suspended", "Suspended Epsilon"),
                     ("removed", "Removed Echo"),
                     ("outsider", "Outsider Foxtrot"),
                     ("otherOrgMember", "OtherOrg Golf")
                 })
        {
            var email = $"{key}-{suffix}@example.test";
            var user = User.Create(email, displayName, "hash", Now);
            user.MarkEmailVerified(Now);
            context.Users.Add(user);
            users[key] = user.Id;
            emails[key] = email;
        }

        var organization = Organization.Create($"Org {suffix}", $"org-{suffix}", users["owner"], Now);
        var otherOrganization = Organization.Create($"Other {suffix}", $"other-{suffix}", users["otherOrgMember"], Now);
        context.Organizations.AddRange(organization, otherOrganization);

        context.OrganizationMembers.AddRange(
            OrganizationMember.CreateOwner(organization.Id, users["owner"], Now),
            OrganizationMember.Create(organization.Id, users["admin"], OrganizationRole.OrganizationAdmin, users["owner"], Now),
            OrganizationMember.Create(organization.Id, users["member"], OrganizationRole.OrganizationMember, users["owner"], Now),
            OrganizationMember.Create(organization.Id, users["secondaryMember"], OrganizationRole.OrganizationMember, users["owner"], Now),
            OrganizationMember.CreateOwner(otherOrganization.Id, users["otherOrgMember"], Now));

        var suspendedMembership = OrganizationMember.Create(
            organization.Id,
            users["suspended"],
            OrganizationRole.OrganizationMember,
            users["owner"],
            Now);
        suspendedMembership.Change(OrganizationRole.OrganizationMember, MembershipStatus.Suspended, users["owner"], suspendedMembership.Version, Now);
        context.OrganizationMembers.Add(suspendedMembership);

        var removedMembership = OrganizationMember.Create(
            organization.Id,
            users["removed"],
            OrganizationRole.OrganizationMember,
            users["owner"],
            Now);
        removedMembership.Change(OrganizationRole.OrganizationMember, MembershipStatus.Removed, users["owner"], removedMembership.Version, Now);
        context.OrganizationMembers.Add(removedMembership);

        var projectOne = Project.Create(organization.Id, $"Project One {suffix}", $"proj-one-{suffix}", null, "4.3", ProjectVisibility.Private, users["owner"], Now);
        var projectTwo = Project.Create(organization.Id, $"Project Two {suffix}", $"proj-two-{suffix}", null, "4.3", ProjectVisibility.Private, users["owner"], Now);
        context.Projects.AddRange(projectOne, projectTwo);
        context.ProjectSettings.AddRange(ProjectSetting.Create(projectOne.Id, Now), ProjectSetting.Create(projectTwo.Id, Now));

        context.ProjectMembers.AddRange(
            ProjectMember.Create(projectOne.Id, organization.Id, users["owner"], ProjectRole.ProjectOwner, ProjectMemberSource.Direct, users["owner"], Now),
            ProjectMember.Create(projectOne.Id, organization.Id, users["member"], ProjectRole.Developer, ProjectMemberSource.Direct, users["owner"], Now),
            ProjectMember.Create(projectTwo.Id, organization.Id, users["owner"], ProjectRole.ProjectOwner, ProjectMemberSource.Direct, users["owner"], Now),
            ProjectMember.Create(projectTwo.Id, organization.Id, users["member"], ProjectRole.Viewer, ProjectMemberSource.Direct, users["owner"], Now));

        await context.SaveChangesAsync();
        return new SeedData(suffix, organization.Id, projectOne.Id, projectTwo.Id, users, emails);
    }

    private sealed record SeedData(
        string Suffix,
        Guid OrganizationId,
        Guid ProjectOneId,
        Guid ProjectTwoId,
        Dictionary<string, Guid> Users,
        Dictionary<string, string> Emails);
}
