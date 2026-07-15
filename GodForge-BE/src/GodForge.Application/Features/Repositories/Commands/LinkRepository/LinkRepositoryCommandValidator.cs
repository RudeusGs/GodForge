using FluentValidation;
using GodForge.Domain.Enums;

namespace GodForge.Application.Features.Repositories.Commands.LinkRepository;

public sealed class LinkRepositoryCommandValidator : AbstractValidator<LinkRepositoryCommand>
{
    public LinkRepositoryCommandValidator()
    {
        RuleFor(x => x.ProjectId).NotEmpty();
        RuleFor(x => x.RemoteUrl)
            .NotEmpty()
            .MaximumLength(800)
            .Must(IsSupportedUrl)
            .WithMessage("RemoteUrl must be an absolute HTTPS URL without embedded credentials.");
        RuleFor(x => x.Provider)
            .NotEmpty()
            .IsEnumName(typeof(GitProvider), caseSensitive: false);
        RuleFor(x => x.DefaultBranch).NotEmpty().MaximumLength(150);
        RuleFor(x => x.ExternalRepositoryId).MaximumLength(200);
    }

    private static bool IsSupportedUrl(string value)
    {
        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri))
        {
            return false;
        }

        return uri.Scheme == Uri.UriSchemeHttps &&
               string.IsNullOrEmpty(uri.UserInfo);
    }
}

