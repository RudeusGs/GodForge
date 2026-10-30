using GodForge.Domain.Entities.Core;

namespace GodForge.Application.Common.Interfaces.Repositories;

public interface IOrganizationSettingRepository
{
    Task<OrganizationSetting?> GetAsync(Guid organizationId, CancellationToken cancellationToken);
    Task AddAsync(OrganizationSetting setting, CancellationToken cancellationToken);
    Task<bool> GetAiAllowedAsync(Guid organizationId, CancellationToken cancellationToken);
}
