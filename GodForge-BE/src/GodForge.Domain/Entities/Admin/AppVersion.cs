using GodForge.Domain.Common;

namespace GodForge.Domain.Entities.Admin;

public sealed class AppVersion : BaseEntity
{
    public string ServiceName { get; private set; } = default!;
    public string Version { get; private set; } = default!;
    public string? GitSha { get; private set; }
    public DateTimeOffset DeployedAt { get; private set; }
    public string? DeployedBy { get; private set; }

    private AppVersion() { } // EF Core

    public static AppVersion Create(
        string serviceName, string version, string? gitSha,
        DateTimeOffset deployedAt, string? deployedBy)
    {
        return new AppVersion
        {
            Id = Guid.NewGuid(),
            ServiceName = serviceName,
            Version = version,
            GitSha = gitSha,
            DeployedAt = deployedAt,
            DeployedBy = deployedBy
        };
    }
}
