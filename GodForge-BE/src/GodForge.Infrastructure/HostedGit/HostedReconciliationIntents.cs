using System.Text.Json;
using GodForge.Application.Common.Constants;
using GodForge.Application.Common.Models.Messages;
using GodForge.Domain.Entities.Core;
using GodForge.Domain.Entities.Identity;
using GodForge.Domain.Entities.Ops;
using GodForge.Domain.Entities.Repo;
using GodForge.Domain.Enums;
using GodForge.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GodForge.Infrastructure.HostedGit;

// Persistence adapter for transactional side effects. Role decisions are made in Application.
public static class HostedReconciliationIntents
{
    public static async Task StageChangesAsync(GodForgeDbContext context, CancellationToken cancellationToken)
    {
        context.ChangeTracker.DetectChanges();
        var changed = context.ChangeTracker.Entries().Where(e => e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted).ToArray();
        var projectIds = changed.Where(e => e.Entity is Project or ProjectMember).Select(e => e.Entity is Project p ? p.Id : ((ProjectMember)e.Entity).ProjectId).Distinct().ToArray();
        var organizationIds = changed.Where(e => e.Entity is Organization or OrganizationMember).Select(e => e.Entity is Organization o ? o.Id : ((OrganizationMember)e.Entity).OrganizationId).Distinct().ToArray();
        var userIds = changed.Where(e => e.Entity is ForgejoIdentity || e.Entity is User &&
            (e.Property(nameof(User.Status)).IsModified || e.Property(nameof(User.DeletedAt)).IsModified))
            .Select(e => e.Entity is ForgejoIdentity i ? i.UserId : ((User)e.Entity).Id).Distinct().ToArray();
        var provisioned = changed.Where(e => e.Entity is GitRepository r && r.HostedRepositoryId is not null &&
            (e.State == EntityState.Added || e.Property(nameof(GitRepository.HostedRepositoryId)).IsModified || e.Property(nameof(GitRepository.DeletedAt)).IsModified || e.Property(nameof(GitRepository.GitRepositoryStatus)).IsModified))
            .Select(e => (GitRepository)e.Entity).ToArray();
        if (projectIds.Length + organizationIds.Length + userIds.Length + provisioned.Length == 0) return;
        var repositories = await context.GitRepositories.IgnoreQueryFilters().Where(r => r.HostedRepositoryId != null &&
            (projectIds.Contains(r.ProjectId) || context.Projects.IgnoreQueryFilters().Any(p => p.Id == r.ProjectId && organizationIds.Contains(p.OrganizationId)) ||
             context.ProjectMembers.Any(m => m.ProjectId == r.ProjectId && userIds.Contains(m.UserId))))
            .ToListAsync(cancellationToken);
        foreach (var repository in repositories.Concat(provisioned).DistinctBy(r => r.Id))
        {
            var organizationId = await context.Projects.IgnoreQueryFilters().Where(p => p.Id == repository.ProjectId).Select(p => p.OrganizationId).SingleAsync(cancellationToken);
            Stage(context, repository, organizationId, DateTimeOffset.UtcNow);
        }
    }

    public static void Stage(GodForgeDbContext context, GitRepository repository, Guid organizationId, DateTimeOffset now)
    {
        repository.RequestAccessReconciliation(now);
        var job = Job.Create(repository.ProjectId, repository.Id, JobType.ReconcileHostedRepository,
            WorkerQueueNames.HostedRepositoryReconcile, 0, null, null, 5, null, Guid.NewGuid().ToString("N"), now, now);
        var message = new HostedRepositoryReconcileJobMessage
        {
            JobId = job.Id,
            ProjectId = repository.ProjectId,
            OrganizationId = organizationId,
            RepositoryId = repository.Id,
            InputHash = $"access:{repository.Id:N}",
            CorrelationId = job.CorrelationId,
            CreatedAt = now
        };
        context.Jobs.Add(job);
        context.OutboxMessages.Add(OutboxMessage.Create("repository", repository.Id, WorkerQueueNames.HostedRepositoryReconcile,
            JsonSerializer.Serialize(message, new JsonSerializerOptions(JsonSerializerDefaults.Web)), null, job.CorrelationId, now));
    }
}
