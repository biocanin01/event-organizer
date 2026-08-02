using MediatR;

namespace EventOrganizer.Application.Commands.RejectOrganizerRoleRequest
{
    public sealed record RejectOrganizerRoleRequestCommand(
        Guid RequestId,
        string DecisionReason,
        int Version)
        : IRequest;
}
