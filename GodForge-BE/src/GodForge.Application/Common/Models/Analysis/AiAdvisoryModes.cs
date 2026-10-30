namespace GodForge.Application.Common.Models.Analysis;

// Legacy wire fields named AnalysisProfile select optional AI advisory intent.
// They never identify a deterministic Settings.AnalysisProfileRegistry definition.
public static class AiAdvisoryModes
{
    public const string HealthOverview = "health_overview";
    public const string CommitReview = "commit_review";
    public const string ArchitectureReview = "architecture_review";

    public static bool IsSupported(string mode)
        => mode is HealthOverview or CommitReview or ArchitectureReview;
}
