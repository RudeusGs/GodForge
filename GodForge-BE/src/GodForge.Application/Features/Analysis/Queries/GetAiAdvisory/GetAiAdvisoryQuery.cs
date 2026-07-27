using GodForge.Application.Common.Models;
using GodForge.Application.Features.Analysis.DTOs;
using MediatR;

namespace GodForge.Application.Features.Analysis.Queries.GetAiAdvisory;

public sealed record GetAiAdvisoryQuery(Guid ProjectId, Guid ActorId) : IRequest<Result<AiAdvisoryResponseDto>>;
