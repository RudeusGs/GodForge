using GodForge.Domain.Common;

namespace GodForge.Domain.Entities.Analysis;

public sealed class AiAnalysisRun : BaseEntity
{
    public Guid ProjectId { get; private set; }
    public Guid RepositoryId { get; private set; }
    public string CommitSha { get; private set; } = default!;
    public string AnalysisProfile { get; private set; } = default!;
    public string Provider { get; private set; } = default!;
    public string Model { get; private set; } = default!;
    public string PromptVersion { get; private set; } = default!;
    public string InputHash { get; private set; } = default!;
    public string Status { get; private set; } = default!;
    public string? Summary { get; private set; }
    public string? RawArtifactKey { get; private set; }
    public int? InputTokenCount { get; private set; }
    public int? OutputTokenCount { get; private set; }
    public decimal? EstimatedCost { get; private set; }
    public DateTimeOffset StartedAt { get; private set; }
    public DateTimeOffset? CompletedAt { get; private set; }
    public string? ErrorCode { get; private set; }

    private AiAnalysisRun() { }

    public static AiAnalysisRun Create(
        Guid projectId,
        Guid repositoryId,
        string commitSha,
        string analysisProfile,
        string provider,
        string model,
        string promptVersion,
        string inputHash,
        DateTimeOffset now)
    {
        return new AiAnalysisRun
        {
            Id = Guid.NewGuid(),
            ProjectId = projectId,
            RepositoryId = repositoryId,
            CommitSha = commitSha,
            AnalysisProfile = analysisProfile,
            Provider = provider,
            Model = model,
            PromptVersion = promptVersion,
            InputHash = inputHash,
            Status = "running",
            StartedAt = now
        };
    }

    public void MarkCompleted(
        string summary,
        int? inputTokenCount,
        int? outputTokenCount,
        decimal? estimatedCost,
        string? rawArtifactKey,
        DateTimeOffset now)
    {
        Status = "completed";
        Summary = summary;
        InputTokenCount = inputTokenCount;
        OutputTokenCount = outputTokenCount;
        EstimatedCost = estimatedCost;
        RawArtifactKey = rawArtifactKey;
        ErrorCode = null;
        CompletedAt = now;
    }

    public void MarkFailed(string errorCode, DateTimeOffset now)
    {
        Status = "failed";
        ErrorCode = errorCode;
        CompletedAt = now;
    }
}
