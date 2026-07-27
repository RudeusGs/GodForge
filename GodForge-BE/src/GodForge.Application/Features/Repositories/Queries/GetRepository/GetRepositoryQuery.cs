using GodForge.Application.Common.Models;
using GodForge.Application.Features.Repositories.DTOs;
using MediatR;

namespace GodForge.Application.Features.Repositories.Queries.GetRepository;

public sealed record GetRepositoryQuery(Guid ProjectId, Guid ActorId) : IRequest<Result<RepositoryDto>>;
