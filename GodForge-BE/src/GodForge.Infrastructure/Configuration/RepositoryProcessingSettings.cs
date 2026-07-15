namespace GodForge.Infrastructure.Configuration;

public sealed class RepositoryProcessingSettings
{
    public string WorkspaceRoot { get; set; } = ".workspaces";
    public long MaxRepositoryBytes { get; set; } = 500L * 1024 * 1024;
    public int MaxTextFileBytes { get; set; } = 512 * 1024;
    public int MaxContextCharacters { get; set; } = 1_500_000;
    public int MaxFiles { get; set; } = 20_000;
    public int GitCommandTimeoutSeconds { get; set; } = 300;
}
