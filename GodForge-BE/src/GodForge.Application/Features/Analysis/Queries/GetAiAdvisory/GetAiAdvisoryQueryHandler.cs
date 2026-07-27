using GodForge.Application.Common.Interfaces.Repositories;
using GodForge.Application.Common.Models;
using GodForge.Application.Common.Security;
using GodForge.Application.Features.Analysis.DTOs;
using MediatR;

namespace GodForge.Application.Features.Analysis.Queries.GetAiAdvisory;

public sealed class GetAiAdvisoryQueryHandler : IRequestHandler<GetAiAdvisoryQuery, Result<AiAdvisoryResponseDto>>
{
    private readonly IProjectRepository _projects;
    private readonly IAiAnalysisRepository _aiRepository;
    private readonly IAuthorizationService _authorization;

    public GetAiAdvisoryQueryHandler(
        IProjectRepository projects,
        IAiAnalysisRepository aiRepository,
        IAuthorizationService authorization)
    {
        _projects = projects;
        _aiRepository = aiRepository;
        _authorization = authorization;
    }

    public async Task<Result<AiAdvisoryResponseDto>> Handle(GetAiAdvisoryQuery request, CancellationToken cancellationToken)
    {
        if (!await _authorization.HasPermissionAsync(
                request.ActorId,
                request.ProjectId,
                Permissions.AnalysisRead,
                cancellationToken))
        {
            return Result<AiAdvisoryResponseDto>.Failure(
                ApplicationError.Forbidden("SECURITY_FORBIDDEN", "You do not have permission to view this AI advisory."));
        }

        var project = await _projects.GetByIdAsync(request.ProjectId, cancellationToken);
        if (project is null)
        {
            return Result<AiAdvisoryResponseDto>.Failure(ApplicationError.NotFound("PROJECT_NOT_FOUND", "Project not found."));
        }

        var run = await _aiRepository.GetLatestByProjectAsync(request.ProjectId, cancellationToken);
        if (run is null)
        {
            return Result<AiAdvisoryResponseDto>.Failure(ApplicationError.NotFound("ADVISORY_NOT_FOUND", "No AI advisory found for this project."));
        }

        var findings = await _aiRepository.GetFindingsByRunAsync(run.Id, cancellationToken);

        var advisoryDto = new AiAdvisoryDto(
            run.Id,
            run.ProjectId,
            run.RepositoryId,
            run.CommitSha,
            run.Provider,
            run.Model,
            run.Status,
            run.Summary,
            run.StartedAt,
            run.CompletedAt);

        var findingsDto = findings.Select(f => new AiFindingDto(
            f.Id,
            f.Category,
            f.Severity,
            f.Title,
            f.Description,
            f.Recommendation,
            f.Confidence,
            f.EvidenceRefsJson,
            f.Status,
            f.CreatedAt)).ToList();

        return Result<AiAdvisoryResponseDto>.Success(new AiAdvisoryResponseDto(advisoryDto, findingsDto));
    }
}
