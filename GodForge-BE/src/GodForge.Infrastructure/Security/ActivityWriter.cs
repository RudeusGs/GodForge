using GodForge.Application.Common.Interfaces;
using GodForge.Application.Common.Interfaces.Repositories;
using GodForge.Domain.Entities.Collab;
using GodForge.Domain.Enums;

namespace GodForge.Infrastructure.Security;

public sealed class ActivityWriter : IActivityWriter
{
    private readonly IActivityRepository _activityRepository;
    private readonly IClock _clock;

    public ActivityWriter(IActivityRepository activityRepository, IClock clock)
    {
        _activityRepository = activityRepository;
        _clock = clock;
    }

    public async Task WriteAsync(
        Guid projectId,
        Guid? actorId,
        string action,
        string? targetType,
        string? targetId,
        ActivityStatus status,
        string correlationId,
        string? metadataJson = null,
        CancellationToken cancellationToken = default)
    {
        var activity = Activity.Create(
            projectId,
            null,
            actorId,
            action,
            targetType,
            targetId,
            status.ToString(),
            metadataJson,
            correlationId,
            _clock.UtcNow);

        await _activityRepository.AddAsync(activity, cancellationToken);
    }
}
