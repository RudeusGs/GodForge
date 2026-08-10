namespace GodForge.Api.Contracts.Projects;

public sealed record UpdateProjectRequest(string Name, string Slug, string? Description, string Visibility, long Version);
public sealed record DeleteProjectRequest(long Version, string ConfirmationSlug);
public sealed record RestoreProjectRequest(long Version);
public sealed record TransferProjectOwnershipRequest(Guid NewOwnerUserId, string RetainCurrentOwnerAs, long Version);
public sealed record AddProjectMemberRequest(Guid UserId, string Role);
public sealed record UpdateProjectMemberRequest(string Role, long Version);
public sealed record UpdateProjectSettingsRequest(
    string AnalysisProfileKey,
    bool AiAdvisoryEnabled,
    string DefaultAssetVisibility,
    int NotificationPolicyVersion,
    long Version);
