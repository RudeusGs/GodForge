using FluentValidation;
using GodForge.Application.Common.Interfaces;
using GodForge.Application.Common.Interfaces.Repositories;
using GodForge.Application.Common.Models;
using GodForge.Application.Common.Text;
using GodForge.Domain.Entities.Identity;
using GodForge.Domain.Enums;
using MediatR;

namespace GodForge.Application.Features.Settings;

public sealed record UserPreferencesDto(string Theme, bool NotificationInApp, bool NotificationEmail,
    string NotificationDigest, long Version, DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt)
{
    public static UserPreferencesDto From(UserSetting s) => new(EnumText.ToCamelCase(s.Theme),
        s.NotificationInApp, s.NotificationEmail, EnumText.ToCamelCase(s.NotificationDigest), s.Version, s.CreatedAt, s.UpdatedAt);
}

public sealed record GetUserPreferencesQuery(Guid ActorId) : IRequest<Result<UserPreferencesDto>>;
public sealed record UpdateUserPreferencesCommand(Guid ActorId, string Theme, bool NotificationInApp,
    bool NotificationEmail, string NotificationDigest, long Version) : IRequest<Result<UserPreferencesDto>>;

public sealed class UpdateUserPreferencesValidator : AbstractValidator<UpdateUserPreferencesCommand>
{
    public UpdateUserPreferencesValidator()
    {
        RuleFor(r => r.ActorId).NotEmpty();
        RuleFor(r => r.Theme).Must(v => v is "light" or "dark");
        RuleFor(r => r.NotificationDigest).Must(v => v is "off" or "daily" or "weekly");
        RuleFor(r => r.Version).GreaterThan(0);
    }
}

public sealed class UserPreferencesHandler(IUserSettingRepository settings, IUserRepository users,
    IUnitOfWork unitOfWork, IClock clock, IAuditWriter audit) :
    IRequestHandler<GetUserPreferencesQuery, Result<UserPreferencesDto>>,
    IRequestHandler<UpdateUserPreferencesCommand, Result<UserPreferencesDto>>
{
    public Task<Result<UserPreferencesDto>> Handle(GetUserPreferencesQuery request, CancellationToken cancellationToken)
        => ExecuteAsync(request.ActorId, null, cancellationToken);
    public Task<Result<UserPreferencesDto>> Handle(UpdateUserPreferencesCommand request, CancellationToken cancellationToken)
        => ExecuteAsync(request.ActorId, request, cancellationToken);

    private async Task<Result<UserPreferencesDto>> ExecuteAsync(Guid actorId, UpdateUserPreferencesCommand? update, CancellationToken ct)
    {
        await unitOfWork.BeginTransactionAsync(ct);
        try
        {
            await unitOfWork.AcquireResourceLockAsync("user-preferences", actorId, ct);
            unitOfWork.ClearTrackedChanges();
            var user = await users.GetByIdAsync(actorId, ct);
            if (user is not { Status: UserStatus.Active, DeletedAt: null })
            {
                await unitOfWork.RollbackTransactionAsync(ct);
                return ApplicationError.Forbidden("SECURITY_FORBIDDEN", "An active user is required.");
            }
            var setting = await settings.GetAsync(actorId, ct);
            if (setting is null)
            {
                setting = UserSetting.Create(actorId, clock.UtcNow);
                await settings.AddAsync(setting, ct);
            }
            if (update is not null)
            {
                if (setting.Version != update.Version)
                {
                    await unitOfWork.RollbackTransactionAsync(ct);
                    unitOfWork.ClearTrackedChanges();
                    return ApplicationError.Conflict("CONCURRENCY_CONFLICT", "User preferences version is stale.");
                }
                var before = UserPreferencesDto.From(setting);
                setting.Update(Enum.Parse<Theme>(update.Theme, true), update.NotificationInApp,
                    update.NotificationEmail, Enum.Parse<NotificationDigest>(update.NotificationDigest, true), update.Version, clock.UtcNow);
                await audit.WriteAuditAsync(actorId, null, "user.settings_updated", "user-settings", setting.Id,
                    "succeeded", new { before, after = UserPreferencesDto.From(setting) }, ct);
            }
            await unitOfWork.SaveChangesAsync(ct);
            await unitOfWork.CommitTransactionAsync(ct);
            if (update is not null) SettingsMetrics.Updated("user");
            return UserPreferencesDto.From(setting);
        }
        catch (ConcurrencyConflictException)
        {
            await unitOfWork.RollbackTransactionAsync(ct);
            unitOfWork.ClearTrackedChanges();
            return ApplicationError.Conflict("CONCURRENCY_CONFLICT", "User preferences changed concurrently.");
        }
        catch
        {
            await unitOfWork.RollbackTransactionAsync(ct);
            unitOfWork.ClearTrackedChanges();
            throw;
        }
    }
}
