using GodForge.Application.Common.Interfaces;
using GodForge.Application.Common.Interfaces.Repositories;
using GodForge.Application.Common.Models;
using GodForge.Application.Common.Security;
using GodForge.Application.Features.Projects.DTOs;

namespace GodForge.Application.Features.Projects;

public sealed class ProjectSettingsService : ProjectOperationServiceBase, IProjectSettingsService
{
    public ProjectSettingsService(
        IProjectRepository projects,
        IProjectMemberRepository members,
        IOrganizationMemberRepository organizationMembers,
        IAuditWriter auditWriter,
        IClock clock,
        IUnitOfWork unitOfWork)
        : base(projects, members, organizationMembers, auditWriter, clock, unitOfWork)
    {
    }

    public async Task<Result<ProjectSettingsDto>> GetSettingsAsync(Guid actorId, Guid projectId, CancellationToken cancellationToken)
    {
        var access = await GetProjectAccessAsync(actorId, projectId, Permissions.ProjectsRead, false, cancellationToken);
        if (access.Error is not null) return access.Error;
        var settings = await _projects.GetSettingsAsync(projectId, cancellationToken);
        return settings is null
            ? ApplicationError.NotFound("PROJECT_SETTINGS_NOT_FOUND", "Project settings were not found.")
            : ProjectSettingsDto.From(settings);
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
            settings.Update(analysisProfileKey, aiAdvisoryEnabled, defaultAssetVisibility, notificationPolicyVersion, version, _clock.UtcNow);
            await _auditWriter.WriteAuditAsync(
                actorId, projectId, "project.settings_updated", "project-settings", settings.Id, "succeeded",
                new { analysisProfileKey, aiAdvisoryEnabled, defaultAssetVisibility, notificationPolicyVersion, settings.Version }, cancellationToken);
            var save = await SaveAsync(cancellationToken);
            if (save is not null) return await RollbackAsync<ProjectSettingsDto>(save, cancellationToken);
            await _unitOfWork.CommitTransactionAsync(cancellationToken);
            return ProjectSettingsDto.From(settings);
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            _unitOfWork.ClearTrackedChanges();
            throw;
        }
    }
}
