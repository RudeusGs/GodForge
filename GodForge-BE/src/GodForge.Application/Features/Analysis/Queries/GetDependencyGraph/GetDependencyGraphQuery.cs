using GodForge.Application.Common.Models;
using GodForge.Application.Features.Analysis.DTOs;
using MediatR;

namespace GodForge.Application.Features.Analysis.Queries.GetDependencyGraph;

public sealed record GetDependencyGraphQuery(Guid ProjectId, Guid ActorId) : IRequest<Result<DependencyGraphDto>>;
