using GodForge.Application.Common.Interfaces.Repositories;
using GodForge.Application.Common.Models;
using GodForge.Application.Features.Auth.DTOs;
using GodForge.Domain.Enums;
using MediatR;

namespace GodForge.Application.Features.Auth.Queries.GetCurrentUser;

public sealed class GetCurrentUserQueryHandler : IRequestHandler<GetCurrentUserQuery, Result<UserDto>>
{
    private readonly IUserRepository _users;
    public GetCurrentUserQueryHandler(IUserRepository users) => _users = users;

    public async Task<Result<UserDto>> Handle(GetCurrentUserQuery request, CancellationToken cancellationToken)
    {
        var user = await _users.GetByIdAsync(request.UserId, cancellationToken);
        if (user is null)
            return ApplicationError.Unauthorized("UNAUTHORIZED", "Authentication is invalid.");
        if (user.Status != UserStatus.Active)
            return ApplicationError.Forbidden("AUTH_ACCOUNT_DISABLED", "Account is not active.");
        return UserDto.From(user);
    }
}
