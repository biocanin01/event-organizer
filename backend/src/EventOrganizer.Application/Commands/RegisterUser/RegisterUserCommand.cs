using EventOrganizer.Application.Responses;
using MediatR;

namespace EventOrganizer.Application.Commands.RegisterUser
{
    public sealed record RegisterUserCommand(
        string FullName,
        string Email,
        string Password) : IRequest<AuthResponse>;
}