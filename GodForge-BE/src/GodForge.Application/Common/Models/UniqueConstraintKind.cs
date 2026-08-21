namespace GodForge.Application.Common.Models;

public enum UniqueConstraintKind
{
    Unknown = 0,
    AuthChallengeActiveScope,
    UserNormalizedEmail,
    OrganizationSlug,
    UserInviteActiveOrganizationEmail,
    IdempotencyScope,
    ProjectOrganizationSlug,
    ProjectOrganizationName,
    ProjectMemberUser,
    RepositoryProject
}
