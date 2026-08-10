namespace GodForge.Api.Contracts.Repositories;

public sealed record LinkRepositoryRequest(
    string RemoteUrl,
    string Provider,
    string DefaultBranch,
    string? ExternalRepositoryId,
    bool AutoAnalyzeEnabled);

public sealed record TriggerRepositoryAnalysisRequest(
    string? Branch,
    string AnalysisProfile = "health_overview",
    bool IncludeAi = false);
