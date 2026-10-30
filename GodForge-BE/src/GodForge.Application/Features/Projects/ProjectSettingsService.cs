using GodForge.Application.Common.Interfaces;
using GodForge.Application.Common.Interfaces.Repositories;
using GodForge.Application.Common.Models;
using GodForge.Application.Common.Security;
using GodForge.Application.Features.Projects.DTOs;
using GodForge.Application.Features.Settings;
using GodForge.Domain.Entities.Core;
using GodForge.Domain.Enums;

namespace GodForge.Application.Features.Projects;

public sealed class ProjectSettingsService : ProjectOperationServiceBase, IProjectSettingsService
{
    private readonly EffectiveSettingsPolicy _policy;
    public ProjectSettingsService(
        IProjectRepository projects,
        IProjectMemberRepository members,
        IOrganizationMemberRepository organizationMembers,
        IAuditWriter auditWriter,
        IClock clock,
        IUnitOfWork unitOfWork,
        EffectiveSettingsPolicy policy)
        : base(projects, members, organizationMembers, auditWriter, clock, unitOfWork)
    {
        _policy = policy;
    }

    public async Task<Result<ProjectSettingsDto>> GetSettingsAsync(Guid actorId, Guid projectId, CancellationToken cancellationToken)
    {
        var access = await GetProjectAccessAsync(actorId, projectId, Permissions.ProjectsRead, false, cancellationToken);
        if (access.Error is not null) return access.Error;
        var settings = await _projects.GetSettingsAsync(projectId, cancellationToken);
        return settings is null
            ? ApplicationError.NotFound("PROJECT_SETTINGS_NOT_FOUND", "Project settings were not found.")
            : await ToDtoAsync(access.Project!, settings, access.ProjectMembership?.Role, cancellationToken);
    }

    public async Task<Result<ProjectSettingsDto>> UpdateSettingsAsync(
        Guid actorId,
        Guid projectId,
        string analysisProfileKey,
        bool aiAdvisoryEnabled,
        string defaultAssetVisibility,
        int notificationPolicyVersion,
        long version,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(analysisProfileKey) || defaultAssetVisibility is not ("private" or "internal") || notificationPolicyVersion < 1)
            return ApplicationError.Validation("VALIDATION_ERROR", "Project settings are invalid.");
        await BeginSerializedMutationAsync("project-membership", projectId, cancellationToken);
        try
        {
            var access = await GetProjectAccessAsync(actorId, projectId, Permissions.SettingsUpdate, false, cancellationToken);
            if (access.Error is not null) return await RollbackAsync<ProjectSettingsDto>(access.Error, cancellationToken);
            var settings = await _projects.GetSettingsAsync(projectId, cancellationToken);
            if (settings is null)
                return await RollbackAsync<ProjectSettingsDto>(ApplicationError.NotFound("PROJECT_SETTINGS_NOT_FOUND", "Project settings were not found."), cancellationToken);
            if (settings.Version != version)
                return await RollbackAsync<ProjectSettingsDto>(ApplicationError.Conflict("CONCURRENCY_CONFLICT", "Project settings version is stale."), cancellationToken);
            if (!AnalysisProfileRegistry.CanSelect(access.Project!, analysisProfileKey))
                return await RollbackAsync<ProjectSettingsDto>(ApplicationError.Validation("ANALYSIS_PROFILE_INVALID", "Select an active compatible analysis profile."), cancellationToken);
            var effective = await _policy.ResolveAiAsync(access.Project!.OrganizationId, aiAdvisoryEnabled, cancellationToken);
            if (aiAdvisoryEnabled && !effective.Value)
                return await RollbackAsync<ProjectSettingsDto>(ApplicationError.Conflict("SETTINGS_POLICY_RESTRICTED", effective.RestrictionReason!), cancellationToken);
            var before = ProjectSettingsDto.From(settings);
            settings.Update(analysisProfileKey, aiAdvisoryEnabled, defaultAssetVisibility, notificationPolicyVersion, version, _clock.UtcNow);
            await _auditWriter.WriteAuditAsync(
                actorId, projectId, "project.settings_updated", "project-settings", settings.Id, "succeeded",
                new { before, after = ProjectSettingsDto.From(settings) }, cancellationToken);
            var save = await SaveAsync(cancellationToken);
            if (save is not null) return await RollbackAsync<ProjectSettingsDto>(save, cancellationToken);
            await _unitOfWork.CommitTransactionAsync(cancellationToken);
            SettingsMetrics.Updated("project");
            return await ToDtoAsync(access.Project!, settings, access.ProjectMembership?.Role, cancellationToken);
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            _unitOfWork.ClearTrackedChanges();
            throw;
        }
    }

    private async Task<ProjectSettingsDto> ToDtoAsync(Project project, ProjectSetting settings, ProjectRole? role, CancellationToken ct)
    {
        var editable = project.Status == ProjectStatus.Active && role is { } projectRole &&
            RolePermissions.GetPermissionsForRole(projectRole).Contains(Permissions.SettingsUpdate);
        var effective = await _policy.ResolveAiAsync(project.OrganizationId, settings.AiAdvisoryEnabled, ct);
        return ProjectSettingsDto.From(settings) with
        {
            EffectiveAiAdvisory = effective with { Editable = effective.Editable && editable },
            AvailableAnalysisProfiles = AnalysisProfileRegistry.For(project),
            AnalysisProfileVersion = AnalysisProfileRegistry.For(project).FirstOrDefault(p => p.Key == settings.AnalysisProfileKey)?.Version,
            SettingsEditable = editable
        };
    }
}
