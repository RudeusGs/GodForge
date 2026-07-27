using GodForge.Application.Common.Models;
using MediatR;

namespace GodForge.Application.Features.Repositories.Commands.TriggerRepositoryAnalysis;

public sealed record TriggerRepositoryAnalysisCommand(
    Guid ProjectId,
    string? Branch,
    string AnalysisProfile,
    bool IncludeAi,
    Guid ActorId,
    string CorrelationId) : IRequest<Result<Guid>>;
