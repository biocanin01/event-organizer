using MediatR;

namespace EventOrganizer.Application.Commands.SubmitOrganizerRoleRequest
{
    public sealed record SubmitOrganizerRoleRequestCommand(string Motivation)
        : IRequest<Guid>;
}
