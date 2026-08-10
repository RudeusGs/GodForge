namespace GodForge.Api.Contracts.Organizations;

public sealed record CreateOrganizationRequest(string Name, string Slug);
public sealed record UpdateOrganizationRequest(string? Name, string? Slug, long Version);
public sealed record DeleteOrganizationRequest(long Version, string ConfirmationSlug);
public sealed record TransferOrganizationOwnershipRequest(Guid NewOwnerUserId, string RetainCurrentOwnerAs, long Version);
public sealed record UpdateOrganizationMemberRequest(string Role, string Status, long Version);
public sealed record CreateOrganizationInvitationRequest(string Email, string Role);
public sealed record CreateOrganizationProjectRequest(string Name, string Slug, string? Description, string Visibility);
public sealed record AcceptOrganizationInvitationRequest(string Token);
