using FluentValidation;
using GodForge.Application.Common.Models;
using GodForge.Application.Features.Projects.DTOs;
using MediatR;

namespace GodForge.Application.Features.Projects.Commands.TransferProjectOwnership;

public sealed record TransferProjectOwnershipCommand(
    Guid ActorId,
    Guid ProjectId,
    Guid NewOwnerUserId,
    string RetainCurrentOwnerAs,
    long Version) : IRequest<Result<ProjectOwnershipTransferDto>>;

public sealed class TransferProjectOwnershipCommandHandler : IRequestHandler<TransferProjectOwnershipCommand, Result<ProjectOwnershipTransferDto>>
{
    private readonly IProjectManagementService _projects;

    public TransferProjectOwnershipCommandHandler(IProjectManagementService projects) => _projects = projects;

    public Task<Result<ProjectOwnershipTransferDto>> Handle(
        TransferProjectOwnershipCommand request,
        CancellationToken cancellationToken)
        => _projects.TransferOwnershipAsync(
            request.ActorId,
            request.ProjectId,
            request.NewOwnerUserId,
            request.RetainCurrentOwnerAs,
            request.Version,
            cancellationToken);
}

public sealed class TransferProjectOwnershipCommandValidator : AbstractValidator<TransferProjectOwnershipCommand>
{
    public TransferProjectOwnershipCommandValidator()
    {
        RuleFor(request => request.ActorId).NotEmpty();
        RuleFor(request => request.ProjectId).NotEmpty();
        RuleFor(request => request.NewOwnerUserId).NotEmpty();
        RuleFor(request => request.RetainCurrentOwnerAs).NotEmpty().MaximumLength(30);
        RuleFor(request => request.Version).GreaterThan(0);
    }
}
