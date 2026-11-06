using GodForge.Domain.Enums;
using GodForge.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace GodForge.Infrastructure.HostedGit;

public sealed class HostedPermissionDriftService(IServiceScopeFactory scopes, ILogger<HostedPermissionDriftService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(5));
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                using var scope = scopes.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<GodForgeDbContext>();
                var candidates = await context.GitRepositories.IgnoreQueryFilters()
                    .Where(r => r.HostedRepositoryId != null && !context.Jobs.Any(j => j.RepositoryId == r.Id && j.Type == JobType.ReconcileHostedRepository &&
                        (j.Status == JobStatus.Queued || j.Status == JobStatus.Running || j.Status == JobStatus.Retrying)))
                    .OrderBy(r => r.AccessRequestedAt).Take(100).ToListAsync(stoppingToken);
                foreach (var repository in candidates)
                {
                    var organizationId = await context.Projects.IgnoreQueryFilters().Where(p => p.Id == repository.ProjectId).Select(p => p.OrganizationId).SingleAsync(stoppingToken);
                    HostedReconciliationIntents.Stage(context, repository, organizationId, DateTimeOffset.UtcNow);
                }
                await context.SaveChangesAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { return; }
            catch (Exception) { logger.LogError("HOSTED_PERMISSION_DRIFT_SCAN_FAILED"); }
        }
    }
}
