using GodForge.Application.Common.Text;
using GodForge.Domain.Entities.Core;
using GodForge.Domain.Entities.Identity;

namespace GodForge.Application.Features.Organizations.DTOs;

public sealed record OrganizationDto(
    Guid Id,
    string Slug,
    string Name,
    string Status,
    string CurrentUserRole,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    long Version)
{
    public static OrganizationDto From(Organization organization, OrganizationMember membership) => new(
        organization.Id,
        organization.Slug,
        organization.Name,
        EnumText.ToCamelCase(organization.Status),
        EnumText.ToCamelCase(membership.Role),
        organization.CreatedAt,
        organization.UpdatedAt,
        organization.Version);
}

public sealed record OrganizationMemberDto(
    Guid UserId,
    string Email,
    string DisplayName,
    string Role,
    string Status,
    DateTimeOffset JoinedAt,
    long Version)
{
    public static OrganizationMemberDto From(OrganizationMember membership, User user) => new(
        membership.UserId,
        user.Email,
        user.DisplayName,
        EnumText.ToCamelCase(membership.Role),
        EnumText.ToCamelCase(membership.Status),
        membership.JoinedAt,
        membership.Version);
}

public sealed record OrganizationInvitationDto(
    Guid Id,
    string Email,
    string Role,
    string Status,
    DateTimeOffset ExpiresAt,
    Guid InvitedByUserId,
    DateTimeOffset CreatedAt,
    long Version)
{
    public static OrganizationInvitationDto From(UserInvite invitation) => new(
        invitation.Id,
        invitation.Email,
        EnumText.ToCamelCase(invitation.Role),
        EnumText.ToCamelCase(invitation.Status),
        invitation.ExpiresAt,
        invitation.InvitedBy,
        invitation.CreatedAt,
        invitation.Version);
}

public sealed record OrganizationOwnershipTransferDto(
    OrganizationMemberDto PreviousOwner,
    OrganizationMemberDto NewOwner);

public sealed record OrganizationInvitationAcceptanceDto(
    OrganizationDto Organization,
    OrganizationMemberDto Membership);

public sealed record DeletionAcceptedDto(Guid ResourceId, string Status, long Version);
