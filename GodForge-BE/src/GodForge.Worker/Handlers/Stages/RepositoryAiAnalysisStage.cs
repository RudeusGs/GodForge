using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using GodForge.Application.Common.Interfaces;
using GodForge.Application.Common.Interfaces.Repositories;
using GodForge.Application.Common.Models.Analysis;
using GodForge.Application.Common.Models.Messages;
using GodForge.Domain.Entities.Analysis;
using GodForge.Domain.Entities.Repo;

namespace GodForge.Worker.Handlers.Stages;

public sealed record RepositoryAiStageResult(
    AiStageStatus Status,
    string? Summary,
    int FindingCount,
    string? ErrorCode)
{
    public static RepositoryAiStageResult NotRequested() => new(AiStageStatus.NotRequested, null, 0, null);
}

public sealed class RepositoryAiAnalysisStage
{
    private const string PromptVersion = "health-overview-v1";
    private readonly IAiAnalysisRepository _aiRepository;
    private readonly IAiAnalysisProvider _aiProvider;
    private readonly IClock _clock;

    public RepositoryAiAnalysisStage(
        IAiAnalysisRepository aiRepository,
        IAiAnalysisProvider aiProvider,
        IClock clock)
    {
        _aiRepository = aiRepository;
        _aiProvider = aiProvider;
        _clock = clock;
    }

    public async Task<RepositoryAiStageResult> StageAsync(
        RepositoryAnalysisJobMessage message,
        GitRepository repository,
        RepositoryDeterministicStageResult stageResult,
        CancellationToken cancellationToken)
    {
        if (!message.IncludeAi)
            return RepositoryAiStageResult.NotRequested();

        var cachedRun = await _aiRepository.GetCompletedAsync(
            repository.Id,
            stageResult.Sync.CommitSha,
            message.AnalysisProfile,
            _aiProvider.ProviderName,
            _aiProvider.ModelName,
            PromptVersion,
            stageResult.Context.InputHash,
            cancellationToken);

        if (cachedRun is not null)
            return new RepositoryAiStageResult(AiStageStatus.Cached, cachedRun.Summary, 0, null);

        var aiResult = await _aiProvider.AnalyzeAsync(new AiAnalysisRequest(
            message.ProjectId,
            repository.Id,
            stageResult.Sync.CommitSha,
            message.AnalysisProfile,
            PromptVersion,
            stageResult.Context,
            stageResult.Deterministic), cancellationToken);

        if (!aiResult.IsEnabled)
            return new RepositoryAiStageResult(AiStageStatus.Disabled, null, 0, null);

        var run = AiAnalysisRun.Create(
            message.ProjectId,
            repository.Id,
            stageResult.Sync.CommitSha,
            message.AnalysisProfile,
            _aiProvider.ProviderName,
            _aiProvider.ModelName,
            PromptVersion,
            stageResult.Context.InputHash,
            _clock.UtcNow);
        await _aiRepository.AddRunAsync(run, cancellationToken);

        if (!aiResult.IsSuccess)
        {
            var errorCode = aiResult.ErrorCode ?? "AI_ANALYSIS_FAILED";
            run.MarkFailed(errorCode, _clock.UtcNow);
            return new RepositoryAiStageResult(AiStageStatus.Failed, null, 0, errorCode);
        }

        run.MarkCompleted(
            aiResult.Summary ?? string.Empty,
            aiResult.InputTokenCount,
            aiResult.OutputTokenCount,
            null,
            null,
            _clock.UtcNow);

        var fingerprints = new HashSet<string>(StringComparer.Ordinal);
        foreach (var finding in aiResult.Findings)
        {
            var evidenceJson = JsonSerializer.Serialize(finding.EvidenceRefs);
            var fingerprint = CreateFingerprint(finding.Title, evidenceJson);
            if (!fingerprints.Add(fingerprint))
                continue;

            await _aiRepository.AddFindingAsync(AiFinding.Create(
                run.Id,
                finding.Category,
                finding.Severity,
                finding.Title,
                finding.Description,
                finding.Recommendation,
                finding.Confidence,
                evidenceJson,
                fingerprint,
                _clock.UtcNow), cancellationToken);
        }

        return new RepositoryAiStageResult(AiStageStatus.Completed, aiResult.Summary, fingerprints.Count, null);
    }

    private static string CreateFingerprint(string title, string evidenceJson)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(title + "\n" + evidenceJson));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
