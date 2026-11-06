namespace GodForge.Api.Contracts.Repositories;

public sealed record ProvisionHostedRepositoryRequest(string Name, string DefaultBranch = "main", bool IsPrivate = true, bool AutoAnalyzeEnabled = false);

public sealed record LinkRepositoryRequest(
    string RemoteUrl,
    string Provider,
    string DefaultBranch,
    string? ExternalRepositoryId,
    bool AutoAnalyzeEnabled);

// AnalysisProfile is the backwards-compatible AI advisory mode field, not a deterministic profile key.
public sealed record TriggerRepositoryAnalysisRequest(
    string? Branch,
    string AnalysisProfile = "health_overview",
    bool IncludeAi = false);
