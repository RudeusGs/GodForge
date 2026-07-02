using GodForge.Application.Common.Interfaces.Repositories;
using GodForge.Application.Common.Models;
using GodForge.Application.Common.Security;
using GodForge.Application.Features.Jobs.DTOs;
using MediatR;

namespace GodForge.Application.Features.Jobs.Queries.GetProjectJobs;

public sealed class GetProjectJobsQueryHandler : IRequestHandler<GetProjectJobsQuery, Result<PagedResult<JobDto>>>
{
    private readonly IJobRepository _jobs;
    private readonly IAuthorizationService _auth;

    public GetProjectJobsQueryHandler(IJobRepository jobs, IAuthorizationService auth)
    {
        _jobs = jobs;
        _auth = auth;
    }

    public async Task<Result<PagedResult<JobDto>>> Handle(GetProjectJobsQuery request, CancellationToken cancellationToken)
    {
        if (!await _auth.HasPermissionAsync(request.ActorId, request.ProjectId, Permissions.JobsRead, cancellationToken))
            return ApplicationError.Forbidden("FORBIDDEN", "You do not have permission to read jobs in this project.");

        var pagedJobs = await _jobs.GetProjectJobsAsync(request.ProjectId, request.Page, request.PageSize, cancellationToken);
        var dtos = pagedJobs.Items.Select(JobDto.From).ToList();

        var result = new PagedResult<JobDto>(dtos, pagedJobs.Page, pagedJobs.PageSize, pagedJobs.TotalItems);
        return Result<PagedResult<JobDto>>.Success(result);
    }
}
