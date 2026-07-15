using GodForge.Domain.Entities.Core;

namespace GodForge.Application.Features.Projects.DTOs;

public record ProjectDto(
    Guid Id,
    string Name,
    string Slug,
    string? Description,
    string Visibility,
    string Status,
    Guid CreatedBy,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt)
{
    public static ProjectDto From(Project project) => new(
        project.Id,
        project.Name,
        project.Slug,
        project.Description,
        project.Visibility.ToString(),
        project.Status.ToString(),
        project.CreatedBy,
        project.CreatedAt,
        project.UpdatedAt);
}
