namespace GodForge.Application.Common.Models;

public sealed record ProjectMemberStatistics(
    Guid ProjectId,
    int OwnerCount,
    int ActiveMemberCount);
