using MediatR;

namespace EventOrganizer.Application.Commands.LoginUser
{
    public sealed record LoginUserCommand(
        string Email,
        string Password) : IRequest<AuthResponse>;
}
