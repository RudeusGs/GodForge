using GodForge.Domain.Entities.Identity;

namespace GodForge.Application.Common.Interfaces.Repositories;

public interface IUserSettingRepository
{
    Task<UserSetting?> GetAsync(Guid userId, CancellationToken cancellationToken);
    Task AddAsync(UserSetting setting, CancellationToken cancellationToken);
}
