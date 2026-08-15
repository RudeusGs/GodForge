using FluentValidation;
using GodForge.Application.Common.Models;
using GodForge.Application.Features.Projects.DTOs;
using MediatR;

namespace GodForge.Application.Features.Projects.Commands.UpdateProjectSettings;

public sealed record UpdateProjectSettingsCommand(
    Guid ActorId,
    Guid ProjectId,
    string AnalysisProfileKey,
    bool AiAdvisoryEnabled,
    string DefaultAssetVisibility,
    int NotificationPolicyVersion,
    long Version) : IRequest<Result<ProjectSettingsDto>>;

public sealed class UpdateProjectSettingsCommandHandler : IRequestHandler<UpdateProjectSettingsCommand, Result<ProjectSettingsDto>>
{
    private readonly IProjectSettingsService _projects;

    public UpdateProjectSettingsCommandHandler(IProjectSettingsService projects) => _projects = projects;

    public Task<Result<ProjectSettingsDto>> Handle(
        UpdateProjectSettingsCommand request,
        CancellationToken cancellationToken)
        => _projects.UpdateSettingsAsync(
            request.ActorId,
            request.ProjectId,
            request.AnalysisProfileKey,
            request.AiAdvisoryEnabled,
            request.DefaultAssetVisibility,
            request.NotificationPolicyVersion,
            request.Version,
            cancellationToken);
}

public sealed class UpdateProjectSettingsCommandValidator : AbstractValidator<UpdateProjectSettingsCommand>
{
    public UpdateProjectSettingsCommandValidator()
    {
        RuleFor(request => request.ActorId).NotEmpty();
        RuleFor(request => request.ProjectId).NotEmpty();
        RuleFor(request => request.AnalysisProfileKey).NotEmpty().MaximumLength(80);
        RuleFor(request => request.DefaultAssetVisibility).NotEmpty().MaximumLength(32);
        RuleFor(request => request.NotificationPolicyVersion).GreaterThan(0);
        RuleFor(request => request.Version).GreaterThan(0);
    }
}
