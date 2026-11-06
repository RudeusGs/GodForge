using GodForge.Application.Common.Interfaces;
using GodForge.Domain.Entities.Repo;
using GodForge.Domain.Enums;
using GodForge.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GodForge.Infrastructure.HostedGit;

public sealed class HostedRepositoryAccessStore(GodForgeDbContext context) : IHostedRepositoryAccessStore
{
    public Task<ForgejoIdentity?> GetIdentityAsync(Guid userId, CancellationToken cancellationToken)
        => context.Set<ForgejoIdentity>().SingleOrDefaultAsync(x => x.UserId == userId, cancellationToken);
    public async Task AddIdentityAsync(ForgejoIdentity identity, CancellationToken cancellationToken)
        => await context.Set<ForgejoIdentity>().AddAsync(identity, cancellationToken);

    public async Task<HostedAccessScope?> ReadAsync(Guid repositoryId, CancellationToken cancellationToken)
    {
        var repository = await context.GitRepositories.IgnoreQueryFilters().SingleOrDefaultAsync(r => r.Id == repositoryId && r.HostedRepositoryId != null, cancellationToken);
        if (repository is null) return null;
        var project = await context.Projects.IgnoreQueryFilters().SingleAsync(p => p.Id == repository.ProjectId, cancellationToken);
        var organization = await context.Organizations.IgnoreQueryFilters().SingleAsync(o => o.Id == project.OrganizationId, cancellationToken);
        IReadOnlyList<HostedAccessCandidate> members = [];
        if (project.Status == ProjectStatus.Active && organization.Status == OrganizationStatus.Active && repository.DeletedAt is null && repository.GitRepositoryStatus != GitRepositoryStatus.Disabled)
        {
            members = await (from m in context.ProjectMembers
                             join o in context.OrganizationMembers on new { m.OrganizationId, m.UserId } equals new { o.OrganizationId, o.UserId }
                             join u in context.Users on m.UserId equals u.Id
                             join i in context.Set<ForgejoIdentity>() on u.Id equals i.UserId
                             where m.ProjectId == project.Id && m.OrganizationId == project.OrganizationId &&
                                 m.Status == MembershipStatus.Active && o.Status == MembershipStatus.Active && u.Status == UserStatus.Active
                             select new HostedAccessCandidate(i.ProviderUserId, i.Login, m.Role)).Take(10001).ToArrayAsync(cancellationToken);
            if (members.Count > 10000) throw new InvalidOperationException("REPOSITORY_MEMBERSHIP_LIMIT");
        }
        return new(repository, project.OrganizationId, members);
    }
}
