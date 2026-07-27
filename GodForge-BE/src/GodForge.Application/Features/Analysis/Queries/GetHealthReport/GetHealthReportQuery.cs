using GodForge.Application.Common.Models;
using GodForge.Application.Features.Analysis.DTOs;
using MediatR;

namespace GodForge.Application.Features.Analysis.Queries.GetHealthReport;

public sealed record GetHealthReportQuery(Guid ProjectId, Guid ActorId) : IRequest<Result<HealthReportResponseDto>>;

public sealed record HealthReportResponseDto(
    HealthReportDto Report,
    IReadOnlyList<HealthIssueDto> Issues);
