using MediatR;

namespace EventOrganizer.Application.Commands.SuspendUser
{
    public sealed record SuspendUserCommand(Guid UserId)
        : IRequest;
}
