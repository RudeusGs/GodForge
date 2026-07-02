using GodForge.Domain.Entities;
using GodForge.Domain.Entities.Ops;
using GodForge.Domain.Enums;

namespace GodForge.Application.Features.Jobs.DTOs;

public record JobDto(
    Guid Id,
    Guid ProjectId,
    string Type,
    string Status,
    int Progress,
    string? ErrorCode,
    string? ErrorMessage,
    string CorrelationId,
    DateTimeOffset CreatedAt,
    DateTimeOffset? StartedAt,
    DateTimeOffset? CompletedAt)
{
    public static JobDto From(Job job) => new(
        job.Id,
        job.ProjectId,
        job.Type.ToString(),
        job.Status.ToString(),
        job.Progress,
        job.ErrorCode,
        job.ErrorMessage,
        job.CorrelationId,
        job.CreatedAt,
        job.StartedAt,
        job.CompletedAt);
}
