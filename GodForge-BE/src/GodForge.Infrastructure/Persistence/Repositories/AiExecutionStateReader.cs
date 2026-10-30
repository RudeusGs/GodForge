using GodForge.Application.Features.Settings;
using Microsoft.EntityFrameworkCore;

namespace GodForge.Infrastructure.Persistence.Repositories;

public sealed class AiExecutionStateReader(GodForgeDbContext context) : IAiExecutionStateReader
{
    // Projections bypass tracked entities, including repeated checks in a long-lived worker scope.
    public Task<AiExecutionState?> ReadAsync(Guid projectId, CancellationToken ct)
        => (from project in context.Projects.AsNoTracking()
            join organization in context.Organizations.AsNoTracking() on project.OrganizationId equals organization.Id
            join setting in context.ProjectSettings.AsNoTracking() on project.Id equals setting.ProjectId
            where project.Id == projectId && project.DeletedAt == null
            select new AiExecutionState(project.OrganizationId, project.Status, organization.Status, setting.AiAdvisoryEnabled))
            .SingleOrDefaultAsync(ct);
}
