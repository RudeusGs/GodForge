using GodForge.Application.Common.Text;
using GodForge.Domain.Entities.Core;
using GodForge.Domain.Entities.Identity;

namespace GodForge.Application.Features.Projects.DTOs;

public sealed record ProjectDto(
    Guid Id,
    Guid OrganizationId,
    string Slug,
    string Name,
    string? Description,
    string Visibility,
    string Status,
    string CurrentUserRole,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    long Version)
{
    public static ProjectDto From(Project project, ProjectMember membership) =>
        From(project, EnumText.ToCamelCase(membership.Role));

    public static ProjectDto From(Project project, string currentUserRole) => new(
        project.Id,
        project.OrganizationId,
        project.Slug,
        project.Name,
        project.Description,
        EnumText.ToCamelCase(project.Visibility),
        EnumText.ToCamelCase(project.Status),
        currentUserRole,
        project.CreatedAt,
        project.UpdatedAt,
        project.Version);
}

public sealed record ProjectAdministrationDto(
    Guid Id,
    Guid OrganizationId,
    string Slug,
    string Name,
    string Status,
    int OwnerCount,
    int MemberCount,
    DateTimeOffset CreatedAt,
    long Version);

public sealed record ProjectMemberDto(
    Guid UserId,
    string Email,
    string DisplayName,
    string Role,
    string Status,
    DateTimeOffset JoinedAt,
    long Version)
{
    public static ProjectMemberDto From(ProjectMember membership, User user) => new(
        membership.UserId,
        user.Email,
        user.DisplayName,
        EnumText.ToCamelCase(membership.Role),
        EnumText.ToCamelCase(membership.Status),
        membership.JoinedAt,
        membership.Version);
}

public sealed record ProjectOwnershipTransferDto(ProjectMemberDto PreviousOwner, ProjectMemberDto NewOwner);

public sealed record ProjectSettingsDto(
    Guid ProjectId,
    string AnalysisProfileKey,
    bool AiAdvisoryEnabled,
    string DefaultAssetVisibility,
    int NotificationPolicyVersion,
    string SourceScope,
    long Version)
{
    public static ProjectSettingsDto From(ProjectSetting settings) => new(
        settings.ProjectId,
        settings.AnalysisProfileKey,
        settings.AiAdvisoryEnabled,
        settings.DefaultAssetVisibility,
        settings.NotificationPolicyVersion,
        "project",
        settings.Version);
}

public sealed record ProjectDeletionAcceptedDto(Guid ResourceId, string Status, long Version);
