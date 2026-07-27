namespace GodForge.Application.Common.Models.Analysis;

public sealed record WorkspaceSyncResult(
    string WorkspacePath,
    string CommitSha,
    string Branch,
    long RepositorySizeBytes);

public sealed record RepositoryContextArtifact(
    string Content,
    string InputHash,
    int IncludedFileCount,
    int SkippedFileCount,
    bool WasTruncated,
    IReadOnlyList<string> IncludedPaths,
    IReadOnlyList<string> Warnings);

public sealed record DeterministicProjectSummary(
    bool HasProjectFile,
    int TotalFiles,
    int SceneFiles,
    int ResourceFiles,
    int ScriptFiles,
    int TextFiles,
    long TotalBytes,
    IReadOnlyList<DeterministicFinding> Findings);

public sealed record DeterministicFinding(
    string Code,
    string Severity,
    string Message,
    string? FilePath);

public sealed record AiAnalysisRequest(
    Guid ProjectId,
    Guid RepositoryId,
    string CommitSha,
    string AnalysisProfile,
    string PromptVersion,
    RepositoryContextArtifact Context,
    DeterministicProjectSummary DeterministicSummary);

public sealed record AiAnalysisResult(
    bool IsEnabled,
    bool IsSuccess,
    string? Summary,
    IReadOnlyList<AiFindingResult> Findings,
    int? InputTokenCount,
    int? OutputTokenCount,
    string? ErrorCode);

public sealed record AiFindingResult(
    string Category,
    string Severity,
    string Title,
    string Description,
    string? Recommendation,
    decimal? Confidence,
    IReadOnlyList<string> EvidenceRefs);

public sealed record HostedRepositoryProvisionRequest(
    string Owner,
    string Name,
    bool IsPrivate,
    string DefaultBranch);

public sealed record HostedRepositoryProvisionResult(
    string ProviderRepositoryId,
    string CloneUrl,
    string WebUrl,
    string DefaultBranch);
