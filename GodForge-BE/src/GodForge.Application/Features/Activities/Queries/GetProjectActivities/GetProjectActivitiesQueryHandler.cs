using GodForge.Application.Common.Interfaces;
using GodForge.Application.Common.Interfaces.Repositories;
using GodForge.Application.Common.Models;
using GodForge.Application.Common.Security;
using GodForge.Application.Features.Activities.DTOs;
using MediatR;

namespace GodForge.Application.Features.Activities.Queries.GetProjectActivities;

public sealed class GetProjectActivitiesQueryHandler : IRequestHandler<GetProjectActivitiesQuery, Result<PagedResult<ActivityDto>>>
{
    private readonly IActivityRepository _activities;
    private readonly IAuthorizationService _auth;

    public GetProjectActivitiesQueryHandler(IActivityRepository activities, IAuthorizationService auth)
    {
        _activities = activities;
        _auth = auth;
    }

    public async Task<Result<PagedResult<ActivityDto>>> Handle(GetProjectActivitiesQuery request, CancellationToken cancellationToken)
    {
        bool canViewActivities = await _auth.HasPermissionAsync(request.ActorId, request.ProjectId, Permissions.ActivityRead, cancellationToken);
        if (!canViewActivities)
        {
            return Result<PagedResult<ActivityDto>>.Failure(
                ApplicationError.Forbidden("SECURITY_FORBIDDEN", "You do not have permission to view activities for this project.")
            );
        }

        var pagedActivities = await _activities.GetProjectActivitiesAsync(request.ProjectId, request.Page, request.PageSize, cancellationToken);
        var dtos = pagedActivities.Items.Select(ActivityDto.From).ToList();

        var result = new PagedResult<ActivityDto>(dtos, pagedActivities.Page, pagedActivities.PageSize, pagedActivities.TotalItems);
        return Result<PagedResult<ActivityDto>>.Success(result);
    }
}
