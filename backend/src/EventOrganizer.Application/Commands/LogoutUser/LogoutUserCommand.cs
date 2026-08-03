using MediatR;

namespace EventOrganizer.Application.Commands.LogoutUser
{
    public sealed record LogoutUserCommand(string RefreshToken) : IRequest;
}
