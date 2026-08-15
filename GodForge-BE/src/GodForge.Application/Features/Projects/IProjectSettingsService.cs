using GodForge.Application.Common.Models;
using GodForge.Application.Features.Projects.DTOs;

namespace GodForge.Application.Features.Projects;

public interface IProjectSettingsService
{
    Task<Result<ProjectSettingsDto>> GetSettingsAsync(Guid actorId, Guid projectId, CancellationToken cancellationToken);
    Task<Result<ProjectSettingsDto>> UpdateSettingsAsync(Guid actorId, Guid projectId, string analysisProfileKey, bool aiAdvisoryEnabled, string defaultAssetVisibility, int notificationPolicyVersion, long version, CancellationToken cancellationToken);
}
