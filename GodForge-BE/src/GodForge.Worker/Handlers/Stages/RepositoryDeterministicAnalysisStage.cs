using GodForge.Application.Common.Interfaces;
using GodForge.Application.Common.Models.Analysis;
using GodForge.Application.Common.Models.Messages;
using GodForge.Domain.Entities.Repo;

namespace GodForge.Worker.Handlers.Stages;

public sealed record RepositoryDeterministicStageResult(
    WorkspaceSyncResult Sync,
    DeterministicProjectSummary Deterministic,
    RepositoryContextArtifact Context);

public sealed class RepositoryDeterministicAnalysisStage
{
    private readonly IRepositoryWorkspaceService _workspaceService;
    private readonly IDeterministicProjectAnalyzer _deterministicAnalyzer;
    private readonly IRepositoryContextBuilder _contextBuilder;

    public RepositoryDeterministicAnalysisStage(
        IRepositoryWorkspaceService workspaceService,
        IDeterministicProjectAnalyzer deterministicAnalyzer,
        IRepositoryContextBuilder contextBuilder)
    {
        _workspaceService = workspaceService;
        _deterministicAnalyzer = deterministicAnalyzer;
        _contextBuilder = contextBuilder;
    }

    public async Task<RepositoryDeterministicStageResult> ExecuteAsync(
        GitRepository repository,
        RepositoryAnalysisJobMessage message,
        Func<int, CancellationToken, Task> reportProgressAsync,
        CancellationToken cancellationToken)
    {
        var sync = await _workspaceService.SyncAsync(
            repository.Id,
            repository.RemoteUrl,
            message.Branch,
            cancellationToken);
        await reportProgressAsync(30, cancellationToken);

        var deterministic = await _deterministicAnalyzer.AnalyzeAsync(sync.WorkspacePath, cancellationToken);
        await reportProgressAsync(55, cancellationToken);

        var context = await _contextBuilder.BuildAsync(sync.WorkspacePath, cancellationToken);
        await reportProgressAsync(75, cancellationToken);

        return new RepositoryDeterministicStageResult(sync, deterministic, context);
    }
}
