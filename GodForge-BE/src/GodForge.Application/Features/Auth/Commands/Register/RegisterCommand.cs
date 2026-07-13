using GodForge.Application.Common.Models;
using GodForge.Application.Features.Auth.DTOs;
using MediatR;

namespace GodForge.Application.Features.Auth.Commands.Register;

public record RegisterCommand(string Email, string DisplayName, string Password, string Otp) : IRequest<Result<AuthResultDto>>;
