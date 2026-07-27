using System.ComponentModel.DataAnnotations;

namespace GodForge.Infrastructure.Configuration;

public sealed class RepositoryProcessingSettings
{
    [Required]
    public string WorkspaceRoot { get; set; } = ".workspaces";

    [Range(1, long.MaxValue)]
    public long MaxRepositoryBytes { get; set; } = 500L * 1024 * 1024;

    [Range(1, int.MaxValue)]
    public int MaxTextFileBytes { get; set; } = 512 * 1024;

    [Range(1, int.MaxValue)]
    public int MaxContextCharacters { get; set; } = 1_500_000;

    [Range(1, int.MaxValue)]
    public int MaxFiles { get; set; } = 20_000;

    [Range(1, 3600)]
    public int GitCommandTimeoutSeconds { get; set; } = 300;

    public bool AllowPrivateNetworkRemotes { get; set; }
    public string[] AllowedRemoteHosts { get; set; } = Array.Empty<string>();
}
