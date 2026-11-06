using GodForge.Application.Common.Interfaces.Repositories;
using GodForge.Application.Common.Models;
using GodForge.Application.Common.Security;
using GodForge.Application.Features.Jobs.DTOs;
using MediatR;

namespace GodForge.Application.Features.Jobs.Queries.GetJob;

public sealed record GetJobQuery(Guid ProjectId, Guid JobId, Guid ActorId) : IRequest<Result<JobDto>>;
public sealed class GetJobQueryHandler(IJobRepository jobs, IAuthorizationService authorization) : IRequestHandler<GetJobQuery, Result<JobDto>>
{
    public async Task<Result<JobDto>> Handle(GetJobQuery request, CancellationToken token)
    {
        if (!await authorization.HasPermissionAsync(request.ActorId, request.ProjectId, Permissions.JobsRead, token))
            return ApplicationError.NotFound("JOB_NOT_FOUND", "Job was not found.");
        var job = await jobs.GetByIdAsync(request.JobId, token);
        return job?.ProjectId == request.ProjectId ? JobDto.From(job) : ApplicationError.NotFound("JOB_NOT_FOUND", "Job was not found.");
    }
}
