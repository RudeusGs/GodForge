using GodForge.Application.Common.Interfaces.Repositories;
using GodForge.Application.Common.Models;
using GodForge.Application.Common.Security;
using GodForge.Application.Features.Analysis.DTOs;
using MediatR;

namespace GodForge.Application.Features.Analysis.Queries.GetDependencyGraph;

public sealed class GetDependencyGraphQueryHandler : IRequestHandler<GetDependencyGraphQuery, Result<DependencyGraphDto>>
{
    private readonly IProjectRepository _projects;
    private readonly IDependencyGraphSnapshotRepository _graphs;
    private readonly IAuthorizationService _authorization;

    public GetDependencyGraphQueryHandler(
        IProjectRepository projects,
        IDependencyGraphSnapshotRepository graphs,
        IAuthorizationService authorization)
    {
        _projects = projects;
        _graphs = graphs;
        _authorization = authorization;
    }

    public async Task<Result<DependencyGraphDto>> Handle(GetDependencyGraphQuery request, CancellationToken cancellationToken)
    {
        if (!await _authorization.HasPermissionAsync(
                request.ActorId,
                request.ProjectId,
                Permissions.AnalysisRead,
                cancellationToken))
        {
            return Result<DependencyGraphDto>.Failure(
                ApplicationError.Forbidden("SECURITY_FORBIDDEN", "You do not have permission to view this dependency graph."));
        }

        var project = await _projects.GetByIdAsync(request.ProjectId, cancellationToken);
        if (project is null)
        {
            return Result<DependencyGraphDto>.Failure(ApplicationError.NotFound("PROJECT_NOT_FOUND", "Project not found."));
        }

        var snapshot = await _graphs.GetLatestByProjectAsync(request.ProjectId, cancellationToken);
        if (snapshot is null)
        {
            return Result<DependencyGraphDto>.Failure(ApplicationError.NotFound("GRAPH_NOT_FOUND", "No dependency graph found for this project."));
        }

        var nodes = await _graphs.GetNodesBySnapshotAsync(snapshot.Id, cancellationToken);
        var edges = await _graphs.GetEdgesBySnapshotAsync(snapshot.Id, cancellationToken);

        var nodesDto = nodes.Select(n => new DependencyGraphNodeDto(
            n.NodeKey,
            n.NodeType,
            n.FilePath,
            n.Label)).ToList();

        var edgesDto = edges.Select(e => new DependencyGraphEdgeDto(
            e.SourceNodeKey,
            e.TargetNodeKey,
            e.Relation,
            e.Weight)).ToList();

        var dto = new DependencyGraphDto(
            snapshot.Id,
            snapshot.GraphHash,
            snapshot.NodeCount,
            snapshot.EdgeCount,
            snapshot.CycleCount,
            snapshot.CreatedAt,
            nodesDto,
            edgesDto);

        return Result<DependencyGraphDto>.Success(dto);
    }
}
