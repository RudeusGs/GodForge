using GodForge.Application.Common.Interfaces.Repositories;
using GodForge.Domain.Entities.Core;
using Microsoft.EntityFrameworkCore;

namespace GodForge.Infrastructure.Persistence.Repositories;

public sealed class OrganizationSettingRepository(GodForgeDbContext context) : IOrganizationSettingRepository
{
    public Task<OrganizationSetting?> GetAsync(Guid organizationId, CancellationToken cancellationToken)
        => context.OrganizationSettings.SingleOrDefaultAsync(s => s.OrganizationId == organizationId, cancellationToken);
    public Task AddAsync(OrganizationSetting setting, CancellationToken cancellationToken)
        => context.OrganizationSettings.AddAsync(setting, cancellationToken).AsTask();
    public async Task<bool> GetAiAllowedAsync(Guid organizationId, CancellationToken cancellationToken)
        => await context.OrganizationSettings.AsNoTracking().Where(s => s.OrganizationId == organizationId)
            .Select(s => (bool?)s.AiAdvisoryAllowed).SingleOrDefaultAsync(cancellationToken) ?? true;
}
