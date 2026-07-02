using MediatR;
using GodForge.Application.Common.Models;
using GodForge.Application.Features.Projects.DTOs;

namespace GodForge.Application.Features.Projects.Commands.CreateProject;

public record CreateProjectCommand(
    string Name,
    string? Description,
    string Visibility,
    Guid ActorId,
    string CorrelationId) : IRequest<Result<ProjectDto>>;
