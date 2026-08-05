using GodForge.Domain.Enums;

namespace GodForge.Application.Common.Security;

public static class OrganizationRolePermissions
{
    public static IReadOnlySet<string> GetPermissionsForRole(OrganizationRole role) => role switch
    {
        OrganizationRole.OrganizationOwner => new HashSet<string>(StringComparer.Ordinal)
        {
            Permissions.OrganizationsRead, Permissions.OrganizationsUpdate, Permissions.OrganizationsDelete,
            Permissions.OrganizationsTransferOwnership, Permissions.OrganizationMembersRead,
            Permissions.OrganizationMembersInvite, Permissions.OrganizationMembersUpdateRole,
            Permissions.OrganizationMembersRemove, Permissions.OrganizationProjectsListMetadata,
            Permissions.OrganizationProjectsCreate
        },
        OrganizationRole.OrganizationAdmin => new HashSet<string>(StringComparer.Ordinal)
        {
            Permissions.OrganizationsRead, Permissions.OrganizationsUpdate, Permissions.OrganizationMembersRead,
            Permissions.OrganizationMembersInvite, Permissions.OrganizationMembersUpdateRole,
            Permissions.OrganizationMembersRemove, Permissions.OrganizationProjectsListMetadata,
            Permissions.OrganizationProjectsCreate
        },
        OrganizationRole.OrganizationMember => new HashSet<string>(StringComparer.Ordinal)
        {
            Permissions.OrganizationsRead
        },
        _ => new HashSet<string>(StringComparer.Ordinal)
    };
}
