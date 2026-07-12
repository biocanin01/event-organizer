using EventOrganizer.Application.Responses;
using MediatR;

namespace EventOrganizer.Application.Commands.RefreshToken
{
    public sealed record RefreshTokenCommand(
        string RefreshToken) : IRequest<AuthResponse>;
}