using MediatR;
using GodForge.Application.Common.Models;
using GodForge.Application.Features.Activities.DTOs;

namespace GodForge.Application.Features.Activities.Queries.GetProjectActivities;

public record GetProjectActivitiesQuery(
    Guid ProjectId,
    Guid ActorId,
    int Page,
    int PageSize) : IRequest<Result<PagedResult<ActivityDto>>>;
