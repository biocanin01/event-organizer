using MediatR;

namespace EventOrganizer.Application.Commands.ReactivateUser
{
    public sealed record ReactivateUserCommand(Guid UserId)
        : IRequest;
}
