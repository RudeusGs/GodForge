using GodForge.Application.Common.Interfaces.Repositories;
using GodForge.Application.Common.Models;
using GodForge.Application.Features.Analysis.DTOs;
using MediatR;

namespace GodForge.Application.Features.Analysis.Queries.GetHealthReport;

public sealed class GetHealthReportQueryHandler : IRequestHandler<GetHealthReportQuery, Result<HealthReportResponseDto>>
{
    private readonly IProjectRepository _projects;
    private readonly IHealthReportRepository _healthReports;

    public GetHealthReportQueryHandler(IProjectRepository projects, IHealthReportRepository healthReports)
    {
        _projects = projects;
        _healthReports = healthReports;
    }

    public async Task<Result<HealthReportResponseDto>> Handle(GetHealthReportQuery request, CancellationToken cancellationToken)
    {
        var project = await _projects.GetByIdAsync(request.ProjectId, cancellationToken);
        if (project is null)
        {
            return Result<HealthReportResponseDto>.Failure(ApplicationError.NotFound("PROJECT_NOT_FOUND", "Project not found."));
        }

        var report = await _healthReports.GetLatestByProjectAsync(request.ProjectId, cancellationToken);
        if (report is null)
        {
            return Result<HealthReportResponseDto>.Failure(ApplicationError.NotFound("REPORT_NOT_FOUND", "No health report found for this project."));
        }

        var issues = await _healthReports.GetIssuesByReportAsync(report.Id, cancellationToken);

        var reportDto = new HealthReportDto(
            report.Id,
            report.ProjectId,
            report.RepositoryId,
            report.Score,
            report.TotalIssues,
            report.CriticalCount,
            report.WarningCount,
            report.InfoCount,
            report.CreatedAt);

        var issuesDto = issues.Select(i => new HealthIssueDto(
            i.Id,
            i.IssueType,
            i.Severity,
            i.FilePath,
            i.NodePath,
            i.Message,
            i.IsSuppressed)).ToList();

        return Result<HealthReportResponseDto>.Success(new HealthReportResponseDto(reportDto, issuesDto));
    }
}
