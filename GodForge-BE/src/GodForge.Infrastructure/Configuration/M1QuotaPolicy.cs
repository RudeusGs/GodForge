using GodForge.Application.Common.Interfaces;
using Microsoft.Extensions.Options;

namespace GodForge.Infrastructure.Configuration;

public sealed class M1QuotaPolicy : IM1QuotaPolicy
{
    private readonly M1QuotaSettings _settings;

    public M1QuotaPolicy(IOptions<M1QuotaSettings> options) => _settings = options.Value;

    public int MaxOrganizationsPerUser => _settings.MaxOrganizationsPerUser;
    public int MaxProjectsPerOrganization => _settings.MaxProjectsPerOrganization;
    public int MaxPendingInvitationsPerOrganization => _settings.MaxPendingInvitationsPerOrganization;
}
