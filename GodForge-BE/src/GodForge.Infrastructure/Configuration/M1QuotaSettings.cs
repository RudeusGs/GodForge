using System.ComponentModel.DataAnnotations;

namespace GodForge.Infrastructure.Configuration;

public sealed class M1QuotaSettings
{
    public const string SectionName = "M1Quotas";

    [Range(1, 1000)]
    public int MaxOrganizationsPerUser { get; init; } = 10;

    [Range(1, 10000)]
    public int MaxProjectsPerOrganization { get; init; } = 100;

    [Range(1, 10000)]
    public int MaxPendingInvitationsPerOrganization { get; init; } = 100;
}
