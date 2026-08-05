using System.Net;
using GodForge.Domain.Entities.Core;
using GodForge.Domain.Entities.Identity;
using GodForge.Domain.Enums;
using GodForge.IntegrationTests.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace GodForge.IntegrationTests.Projects;

public sealed class TenantRoleMatrixIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly TenantScenarioStore _scenario;

    public TenantRoleMatrixIntegrationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _scenario = factory.Services.GetRequiredService<TenantScenarioStore>();
    }

    public static IEnumerable<object[]> ActiveRoleMatrix()
    {
        foreach (var organizationRole in Enum.GetValues<OrganizationRole>())
        foreach (var projectRole in Enum.GetValues<ProjectRole>())
            yield return new object[] { organizationRole, projectRole };
    }

    [Theory]
    [MemberData(nameof(ActiveRoleMatrix))]
    public async Task GetProject_AllProjectRolesRequireActiveMembershipInSameOrganization(
        OrganizationRole organizationRole,
        ProjectRole projectRole)
    {
        var scenario = BuildScenario(organizationRole, projectRole, MembershipStatus.Active, sameOrganization: true, includeProjectMembership: true);
        using var client = CreateAuthenticatedClient(scenario.User.Id);

        var response = await client.GetAsync($"/api/v1/projects/{scenario.Project.Id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Theory]
    [InlineData(MembershipStatus.Suspended)]
    [InlineData(MembershipStatus.Removed)]
    public async Task GetProject_InactiveOrganizationMembership_IsMaskedAsNotFound(MembershipStatus status)
    {
        var scenario = BuildScenario(OrganizationRole.OrganizationOwner, ProjectRole.ProjectOwner, status, sameOrganization: true, includeProjectMembership: true);
        using var client = CreateAuthenticatedClient(scenario.User.Id);

        var response = await client.GetAsync($"/api/v1/projects/{scenario.Project.Id}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetProject_CrossTenantOrganizationMembership_IsMaskedAsNotFound()
    {
        var scenario = BuildScenario(OrganizationRole.OrganizationOwner, ProjectRole.ProjectOwner, MembershipStatus.Active, sameOrganization: false, includeProjectMembership: true);
        using var client = CreateAuthenticatedClient(scenario.User.Id);

        var response = await client.GetAsync($"/api/v1/projects/{scenario.Project.Id}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Theory]
    [InlineData(OrganizationRole.OrganizationOwner)]
    [InlineData(OrganizationRole.OrganizationAdmin)]
    [InlineData(OrganizationRole.OrganizationMember)]
    public async Task GetProject_OrganizationRoleWithoutProjectMembership_IsForbidden(OrganizationRole organizationRole)
    {
        var scenario = BuildScenario(organizationRole, ProjectRole.Viewer, MembershipStatus.Active, sameOrganization: true, includeProjectMembership: false);
        using var client = CreateAuthenticatedClient(scenario.User.Id);

        var response = await client.GetAsync($"/api/v1/projects/{scenario.Project.Id}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    private HttpClient CreateAuthenticatedClient(Guid userId)
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthHandler.UserIdHeader, userId.ToString());
        return client;
    }

    private Scenario BuildScenario(
        OrganizationRole organizationRole,
        ProjectRole projectRole,
        MembershipStatus organizationStatus,
        bool sameOrganization,
        bool includeProjectMembership)
    {
        _scenario.Reset();
        var now = DateTimeOffset.UtcNow;
        var user = User.Create("matrix@example.com", "Matrix User", "hash", now);
        user.MarkEmailVerified(now);
        var projectOrganizationId = Guid.NewGuid();
        var membershipOrganizationId = sameOrganization ? projectOrganizationId : Guid.NewGuid();
        var organizationMembership = OrganizationMember.Create(membershipOrganizationId, user.Id, organizationRole, user.Id, now);
        if (organizationStatus != MembershipStatus.Active)
            organizationMembership.Change(organizationRole, organizationStatus, user.Id, organizationMembership.Version, now);
        var project = Project.Create(projectOrganizationId, "Matrix Project", "matrix-project", null, "4.3", ProjectVisibility.Private, user.Id, now);

        _scenario.Users[user.Id] = user;
        _scenario.Projects[project.Id] = project;
        _scenario.OrganizationMemberships[(membershipOrganizationId, user.Id)] = organizationMembership;
        if (includeProjectMembership)
        {
            var projectMembership = ProjectMember.Create(project.Id, projectOrganizationId, user.Id, projectRole, ProjectMemberSource.Direct, user.Id, now);
            _scenario.ProjectMemberships[(project.Id, user.Id)] = projectMembership;
        }

        return new Scenario(user, project);
    }

    private sealed record Scenario(User User, Project Project);
}
