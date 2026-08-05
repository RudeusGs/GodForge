using GodForge.Domain.Entities.Core;
using GodForge.Domain.Entities.Identity;

namespace GodForge.IntegrationTests.Infrastructure;

public sealed class TenantScenarioStore
{
    public Dictionary<Guid, User> Users { get; } = new();
    public Dictionary<Guid, Project> Projects { get; } = new();
    public Dictionary<(Guid OrganizationId, Guid UserId), OrganizationMember> OrganizationMemberships { get; } = new();
    public Dictionary<(Guid ProjectId, Guid UserId), ProjectMember> ProjectMemberships { get; } = new();

    public void Reset()
    {
        Users.Clear();
        Projects.Clear();
        OrganizationMemberships.Clear();
        ProjectMemberships.Clear();
    }
}
