using System.Text.Json;
using GodForge.Application.Common.Interfaces;
using GodForge.Application.Common.Interfaces.Repositories;
using GodForge.Application.Common.Models.Analysis;
using GodForge.Application.Common.Models.Messages;
using GodForge.Domain.Entities.Analysis;
using GodForge.Domain.Entities.Ops;
using GodForge.Domain.Entities.Repo;

namespace GodForge.Worker.Handlers.Stages;

public sealed class RepositoryAnalysisPersistenceStage
{
    private readonly IRepositorySnapshotRepository _snapshots;
    private readonly IHealthReportRepository _healthReports;
    private readonly IDependencyGraphSnapshotRepository _graphs;
    private readonly IAnalysisRunRepository _runs;
    private readonly IDependencyGraphBuilder _graphBuilder;
    private readonly IClock _clock;

    public RepositoryAnalysisPersistenceStage(
        IRepositorySnapshotRepository snapshots,
        IHealthReportRepository healthReports,
        IDependencyGraphSnapshotRepository graphs,
        IAnalysisRunRepository runs,
        IDependencyGraphBuilder graphBuilder,
        IClock clock)
    {
        _snapshots = snapshots;
        _healthReports = healthReports;
        _graphs = graphs;
        _runs = runs;
        _graphBuilder = graphBuilder;
        _clock = clock;
    }

    public async Task<AnalysisRun> StageAsync(
        RepositoryAnalysisJobMessage message,
        Job job,
        GitRepository repository,
        RepositoryDeterministicStageResult stageResult,
        CancellationToken cancellationToken)
    {
        var existingRun = await _runs.GetByJobIdAsync(job.Id, cancellationToken);
        if (existingRun is not null)
        {
            if (existingRun.ProjectId != message.ProjectId || existingRun.RepositoryId != repository.Id)
                throw new InvalidOperationException("The existing analysis run does not match the claimed job payload.");

            return existingRun;
        }

        var snapshot = await _snapshots.GetByCommitAsync(
            repository.Id,
            stageResult.Sync.CommitSha,
            cancellationToken);
        if (snapshot is null)
        {
            snapshot = RepositorySnapshot.Create(
                repository.Id,
                stageResult.Sync.CommitSha,
                stageResult.Sync.Branch,
                _clock.UtcNow);
            await _snapshots.AddAsync(snapshot, cancellationToken);
        }

        var analysisRun = AnalysisRun.Create(
            message.ProjectId,
            repository.Id,
            snapshot.Id,
            null,
            job.Id,
            _clock.UtcNow);
        await _runs.AddAsync(analysisRun, cancellationToken);

        var graphResult = await _graphBuilder.BuildAsync(
            message.ProjectId,
            repository.Id,
            snapshot.Id,
            analysisRun.Id,
            stageResult.Sync.WorkspacePath,
            cancellationToken);

        await _graphs.AddSnapshotAsync(graphResult.Snapshot, cancellationToken);
        await _graphs.AddNodesAsync(graphResult.Nodes, cancellationToken);
        await _graphs.AddEdgesAsync(graphResult.Edges, cancellationToken);

        var criticals = stageResult.Deterministic.Findings.Count(finding => finding.Severity == "critical");
        var warnings = stageResult.Deterministic.Findings.Count(finding => finding.Severity == "warning");
        var infos = stageResult.Deterministic.Findings.Count(finding => finding.Severity == "info");
        var score = Math.Max(0, 100 - (criticals * 10) - (warnings * 2));

        var healthReport = HealthReport.Create(
            message.ProjectId,
            repository.Id,
            snapshot.Id,
            analysisRun.Id,
            job.Id,
            score,
            stageResult.Deterministic.Findings.Count,
            criticals,
            warnings,
            infos,
            JsonSerializer.Serialize(stageResult.Deterministic),
            _clock.UtcNow);
        await _healthReports.AddReportAsync(healthReport, cancellationToken);

        var issues = stageResult.Deterministic.Findings.Select(finding => HealthIssue.Create(
            healthReport.Id,
            null,
            finding.Code,
            finding.Severity,
            finding.FilePath,
            null,
            finding.Message,
            null,
            false,
            _clock.UtcNow)).ToList();
        await _healthReports.AddIssuesAsync(issues, cancellationToken);

        snapshot.MarkAsReady(JsonSerializer.Serialize(new
        {
            deterministic = stageResult.Deterministic,
            context = new
            {
                stageResult.Context.InputHash,
                stageResult.Context.IncludedFileCount,
                stageResult.Context.SkippedFileCount,
                stageResult.Context.WasTruncated,
                stageResult.Context.Warnings
            }
        }));

        return analysisRun;
    }
}
