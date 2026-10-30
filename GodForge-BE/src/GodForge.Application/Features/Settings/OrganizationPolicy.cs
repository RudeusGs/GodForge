using GodForge.Application.Common.Interfaces;
using GodForge.Application.Common.Interfaces.Repositories;
using GodForge.Application.Common.Models;
using GodForge.Application.Common.Security;
using GodForge.Application.Features.Organizations.Commands;
using GodForge.Domain.Entities.Core;
using MediatR;

namespace GodForge.Application.Features.Settings;

public sealed record OrganizationPolicyDto(Guid OrganizationId, bool AiAdvisoryAllowed, bool PlatformAiAdvisoryAllowed,
    long Version, DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt);
public sealed record GetOrganizationPolicyQuery(Guid ActorId, Guid OrganizationId) : IRequest<Result<OrganizationPolicyDto>>;
public sealed record UpdateOrganizationPolicyCommand(Guid ActorId, Guid OrganizationId, bool AiAdvisoryAllowed, long Version)
    : IRequest<Result<OrganizationPolicyDto>>;

public sealed class OrganizationPolicyHandler(IOrganizationRepository organizations, IOrganizationMemberRepository members,
    IProjectMemberRepository projectMembers, IUnitOfWork unitOfWork, IOrganizationSettingRepository settings,
    IPlatformCapabilities platform, IClock clock, IAuditWriter audit)
    : OrganizationCommandHandlerBase(organizations, members, projectMembers, unitOfWork),
      IRequestHandler<GetOrganizationPolicyQuery, Result<OrganizationPolicyDto>>,
      IRequestHandler<UpdateOrganizationPolicyCommand, Result<OrganizationPolicyDto>>
{
    public Task<Result<OrganizationPolicyDto>> Handle(GetOrganizationPolicyQuery request, CancellationToken ct)
        => ExecuteAsync(request.ActorId, request.OrganizationId, null, ct);
    public Task<Result<OrganizationPolicyDto>> Handle(UpdateOrganizationPolicyCommand request, CancellationToken ct)
        => ExecuteAsync(request.ActorId, request.OrganizationId, request, ct);

    private async Task<Result<OrganizationPolicyDto>> ExecuteAsync(Guid actorId, Guid organizationId, UpdateOrganizationPolicyCommand? update, CancellationToken ct)
    {
        if (update is { Version: <= 0 }) return ApplicationError.Validation("VALIDATION_ERROR", "Version must be positive.");
        await _unitOfWork.BeginTransactionAsync(ct);
        try
        {
            await _unitOfWork.AcquireResourceLockAsync("organization-membership", organizationId, ct);
            _unitOfWork.ClearTrackedChanges();
            var access = await GetActiveAccessAsync(actorId, organizationId, Permissions.OrganizationsUpdate, ct);
            if (access.Error is not null) return await RollbackAsync<OrganizationPolicyDto>(access.Error, ct);
            var policy = await settings.GetAsync(organizationId, ct);
            if (policy is null)
            {
                policy = OrganizationSetting.Create(organizationId, clock.UtcNow);
                await settings.AddAsync(policy, ct);
            }
            if (update is not null)
            {
                if (policy.Version != update.Version)
                    return await RollbackAsync<OrganizationPolicyDto>(ApplicationError.Conflict("CONCURRENCY_CONFLICT", "Organization policy version is stale."), ct);
                // This is a restriction preference, never an enablement grant. Effective platform false always wins.
                var before = new { policy.AiAdvisoryAllowed, policy.Version };
                policy.Update(update.AiAdvisoryAllowed, update.Version, clock.UtcNow);
                await audit.WriteAuditAsync(actorId, null, "organization.settings_updated", "organization-settings", policy.Id,
                    "succeeded", new { organizationId, before, after = new { policy.AiAdvisoryAllowed, policy.Version } }, ct);
            }
            var error = await SaveAsync(ct);
            if (error is not null) return await RollbackAsync<OrganizationPolicyDto>(error, ct);
            await _unitOfWork.CommitTransactionAsync(ct);
            if (update is not null) SettingsMetrics.Updated("organization");
            return new OrganizationPolicyDto(organizationId, policy.AiAdvisoryAllowed, platform.AiAdvisoryAllowed,
                policy.Version, policy.CreatedAt, policy.UpdatedAt);
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync(ct);
            _unitOfWork.ClearTrackedChanges();
            throw;
        }
    }
}
