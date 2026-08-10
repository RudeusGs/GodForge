using GodForge.Domain.Entities.Core;
using GodForge.Domain.Entities.Identity;
using GodForge.Domain.Enums;
using GodForge.Infrastructure.Persistence.Repositories;

namespace GodForge.IntegrationTests.Persistence;

[Collection(PostgresPersistenceCollection.CollectionName)]
public sealed class MembershipPaginationPersistenceTests
{
    private readonly PostgresPersistenceFixture _fixture;
    private readonly DateTimeOffset _now = new(2026, 8, 6, 0, 0, 0, TimeSpan.Zero);

    public MembershipPaginationPersistenceTests(PostgresPersistenceFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task MemberQueries_ExcludeSoftDeletedUsersBeforeCountAndPagination()
    {
        Guid organizationId;
        Guid projectId;
        Guid activeUserId;
        await using (var seedContext = _fixture.CreateContext())
        {
            var suffix = Guid.NewGuid().ToString("N");
            var activeUser = User.Create($"active-{suffix}@example.com", "Active User", "hash", _now);
            var deletedUser = User.Create($"deleted-{suffix}@example.com", "Deleted User", "hash", _now);
            deletedUser.SoftDelete(_now);

            var organization = Organization.Create(
                $"Pagination Organization {suffix}",
                $"pagination-org-{suffix}",
                activeUser.Id,
                _now);
            var project = Project.Create(
                organization.Id,
                $"Pagination Project {suffix}",
                $"pagination-project-{suffix}",
                null,
                Project.UnknownGodotVersion,
                ProjectVisibility.Private,
                activeUser.Id,
                _now);

            var activeOrganizationMember = OrganizationMember.CreateOwner(
                organization.Id,
                activeUser.Id,
                _now);
            var deletedOrganizationMember = OrganizationMember.Create(
                organization.Id,
                deletedUser.Id,
                OrganizationRole.OrganizationMember,
                activeUser.Id,
                _now);
            var activeProjectMember = ProjectMember.Create(
                project.Id,
                organization.Id,
                activeUser.Id,
                ProjectRole.ProjectOwner,
                ProjectMemberSource.Direct,
                activeUser.Id,
                _now);
            var deletedProjectMember = ProjectMember.Create(
                project.Id,
                organization.Id,
                deletedUser.Id,
                ProjectRole.Viewer,
                ProjectMemberSource.Direct,
                activeUser.Id,
                _now);

            seedContext.AddRange(
                activeUser,
                deletedUser,
                organization,
                project,
                activeOrganizationMember,
                deletedOrganizationMember,
                activeProjectMember,
                deletedProjectMember);
            await seedContext.SaveChangesAsync();

            organizationId = organization.Id;
            projectId = project.Id;
            activeUserId = activeUser.Id;
        }

        await using var queryContext = _fixture.CreateContext();
        var organizationMembers = await new OrganizationMemberRepository(queryContext)
            .GetForOrganizationAsync(organizationId, 1, 20, null, null, null);
        var projectMembers = await new ProjectMemberRepository(queryContext)
            .GetForProjectAsync(projectId, 1, 20, null, null, null);

        Assert.Equal(1, organizationMembers.TotalItems);
        Assert.Single(organizationMembers.Items);
        Assert.Equal(activeUserId, organizationMembers.Items[0].UserId);

        Assert.Equal(1, projectMembers.TotalItems);
        Assert.Single(projectMembers.Items);
        Assert.Equal(activeUserId, projectMembers.Items[0].UserId);
    }
}
