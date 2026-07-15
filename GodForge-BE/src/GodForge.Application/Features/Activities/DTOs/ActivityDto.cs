using GodForge.Domain.Entities.Collab;
using GodForge.Domain.Enums;

namespace GodForge.Application.Features.Activities.DTOs;

public record ActivityDto(
    Guid Id,
    Guid ProjectId,
    Guid? ActorId,
    string Action,
    string? TargetType,
    string? TargetId,
    string Status,
    string? MetadataJson,
    string CorrelationId,
    DateTimeOffset CreatedAt)
{
    public static ActivityDto From(Activity activity) => new(
        activity.Id,
        activity.ProjectId,
        activity.ActorId,
        activity.Action,
        activity.TargetType,
        activity.TargetId,
        activity.Status.ToString(),
        activity.MetadataJson,
        activity.CorrelationId,
        activity.CreatedAt);
}
