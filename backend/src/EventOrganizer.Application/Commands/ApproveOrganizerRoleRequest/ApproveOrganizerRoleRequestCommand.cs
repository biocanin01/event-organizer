using MediatR;

namespace EventOrganizer.Application.Commands.ApproveOrganizerRoleRequest
{
    public sealed record ApproveOrganizerRoleRequestCommand(Guid RequestId, int Version)
        : IRequest;
}
