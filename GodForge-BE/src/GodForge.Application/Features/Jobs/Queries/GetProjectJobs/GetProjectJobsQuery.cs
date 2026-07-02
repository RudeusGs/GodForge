using GodForge.Application.Common.Models;
using GodForge.Application.Features.Jobs.DTOs;
using MediatR;

namespace GodForge.Application.Features.Jobs.Queries.GetProjectJobs;

public record GetProjectJobsQuery(
    Guid ProjectId,
    Guid ActorId,
    int Page,
    int PageSize) : IRequest<Result<PagedResult<JobDto>>>;
