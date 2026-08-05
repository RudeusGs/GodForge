namespace GodForge.Application.Common.Interfaces;

public interface IM1QuotaPolicy
{
    int MaxOrganizationsPerUser { get; }
    int MaxProjectsPerOrganization { get; }
    int MaxPendingInvitationsPerOrganization { get; }
}
