using MediatR;

namespace EventOrganizer.Application.Commands.WithdrawOrganizerRoleRequest
{
    public sealed record WithdrawOrganizerRoleRequestCommand(Guid RequestId, int Version)
        : IRequest;
}
