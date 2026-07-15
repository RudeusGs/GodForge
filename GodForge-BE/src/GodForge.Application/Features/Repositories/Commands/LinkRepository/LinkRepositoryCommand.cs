using GodForge.Application.Common.Models;
using GodForge.Application.Features.Repositories.DTOs;
using MediatR;

namespace GodForge.Application.Features.Repositories.Commands.LinkRepository;

public sealed record LinkRepositoryCommand(
    Guid ProjectId,
    string RemoteUrl,
    string Provider,
    string DefaultBranch,
    string? ExternalRepositoryId,
    bool AutoAnalyzeEnabled,
    Guid ActorId,
    string CorrelationId) : IRequest<Result<RepositoryDto>>;
