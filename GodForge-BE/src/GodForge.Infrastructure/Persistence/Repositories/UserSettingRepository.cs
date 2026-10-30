using GodForge.Application.Common.Interfaces.Repositories;
using GodForge.Domain.Entities.Identity;
using Microsoft.EntityFrameworkCore;

namespace GodForge.Infrastructure.Persistence.Repositories;

public sealed class UserSettingRepository(GodForgeDbContext context) : IUserSettingRepository
{
    public Task<UserSetting?> GetAsync(Guid userId, CancellationToken cancellationToken)
        => context.UserSettings.SingleOrDefaultAsync(s => s.UserId == userId, cancellationToken);
    public Task AddAsync(UserSetting setting, CancellationToken cancellationToken)
        => context.UserSettings.AddAsync(setting, cancellationToken).AsTask();
}
