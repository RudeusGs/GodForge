using FluentValidation;
using GodForge.Application.Common.Interfaces;
using GodForge.Application.Common.Models;
using GodForge.Domain.Entities.Repo;
using MediatR;

namespace GodForge.Application.Features.Repositories.Commands.BindForgejoIdentity;

// Deliberately a class: generated record ToString must not include this credential.
public sealed class BindForgejoIdentityCommand(Guid actorId, string personalToken) : IRequest<Result<ForgejoUserDto>>
{
    public Guid ActorId { get; } = actorId;
    public string PersonalToken { get; } = personalToken;
    public override string ToString() => nameof(BindForgejoIdentityCommand);
}
public sealed class BindForgejoIdentityCommandValidator : AbstractValidator<BindForgejoIdentityCommand>
{
    public BindForgejoIdentityCommandValidator() { RuleFor(x => x.PersonalToken).NotEmpty().Length(20, 1000); }
}
public sealed class BindForgejoIdentityCommandHandler(IHostedGitService provider, IHostedRepositoryAccessStore store,
    IUnitOfWork unitOfWork, IClock clock, IAuditWriter audit) : IRequestHandler<BindForgejoIdentityCommand, Result<ForgejoUserDto>>
{
    public async Task<Result<ForgejoUserDto>> Handle(BindForgejoIdentityCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var identity = await provider.VerifyIdentityAsync(request.PersonalToken, cancellationToken);
            var existing = await store.GetIdentityAsync(request.ActorId, cancellationToken);
            if (existing is not null)
                return existing.ProviderUserId == identity.Id ? identity : ApplicationError.Conflict("FORGEJO_IDENTITY_CONFLICT", "An identity is already bound.");
            await store.AddIdentityAsync(ForgejoIdentity.Bind(request.ActorId, identity.Id, identity.Login, clock.UtcNow), cancellationToken);
            await audit.WriteAuditAsync(request.ActorId, null, "forgejo.identity.bound", "user", request.ActorId, "success",
                new { providerUserId = identity.Id }, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return identity;
        }
        catch (UniqueConstraintConflictException) { return ApplicationError.Conflict("FORGEJO_IDENTITY_CONFLICT", "This provider identity is already bound."); }
        catch (HostedGitException e) { return ApplicationError.Validation(e.Code, "Provider identity could not be verified."); }
    }
}
