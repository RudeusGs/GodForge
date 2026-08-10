using System.Net;
using System.Net.Http.Json;
using GodForge.Domain.Entities.Core;
using GodForge.Domain.Entities.Identity;
using GodForge.IntegrationTests.Infrastructure;
using GodForge.IntegrationTests.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GodForge.IntegrationTests.Projects;

[Collection(PostgresPersistenceCollection.CollectionName)]
public sealed class ProjectPersistenceApiTests
{
    private readonly PostgresPersistenceFixture _fixture;
    private readonly DateTimeOffset _now = new(2026, 8, 6, 0, 0, 0, TimeSpan.Zero);

    public ProjectPersistenceApiTests(PostgresPersistenceFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task CreateProject_ConcurrentCaseInsensitiveNames_UsesProductionPersistenceAndReturnsConflict()
    {
        var seeded = await SeedOrganizationAsync();
        using var factory = new PostgresWebApplicationFactory(_fixture.ConnectionString);
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthHandler.UserIdHeader, seeded.OwnerId.ToString());

        var sharedName = $"API Concurrent {Guid.NewGuid():N}";
        var firstRequest = new
        {
            name = sharedName,
            slug = $"api-first-{Guid.NewGuid():N}",
            description = (string?)null,
            visibility = "private"
        };
        var secondRequest = new
        {
            name = sharedName.ToUpperInvariant(),
            slug = $"api-second-{Guid.NewGuid():N}",
            description = (string?)null,
            visibility = "private"
        };

        var responses = await Task.WhenAll(
            client.PostAsJsonAsync($"/api/v1/organizations/{seeded.OrganizationId}/projects", firstRequest),
            client.PostAsJsonAsync($"/api/v1/organizations/{seeded.OrganizationId}/projects", secondRequest));

        Assert.Single(responses, response => response.StatusCode == HttpStatusCode.Created);
        Assert.Single(responses, response => response.StatusCode == HttpStatusCode.Conflict);

        await using var verificationContext = _fixture.CreateContext();
        Assert.Equal(
            1,
            await verificationContext.Projects.CountAsync(project =>
                project.OrganizationId == seeded.OrganizationId &&
                project.Name.ToUpper() == sharedName.ToUpperInvariant()));
    }

    private async Task<SeededOrganization> SeedOrganizationAsync()
    {
        await using var context = _fixture.CreateContext();
        var suffix = Guid.NewGuid().ToString("N");
        var owner = User.Create($"api-owner-{suffix}@example.com", "API Owner", "hash", _now);
        owner.MarkEmailVerified(_now);
        var organization = Organization.Create($"API Organization {suffix}", $"api-org-{suffix}", owner.Id, _now);
        var membership = OrganizationMember.CreateOwner(organization.Id, owner.Id, _now);

        context.AddRange(owner, organization, membership);
        await context.SaveChangesAsync();
        return new SeededOrganization(owner.Id, organization.Id);
    }

    private sealed record SeededOrganization(Guid OwnerId, Guid OrganizationId);
}
