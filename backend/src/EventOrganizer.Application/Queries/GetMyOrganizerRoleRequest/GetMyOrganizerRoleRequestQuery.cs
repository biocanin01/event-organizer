using EventOrganizer.Application.Responses;
using MediatR;

namespace EventOrganizer.Application.Queries.GetMyOrganizerRoleRequest
{
    public sealed record GetMyOrganizerRoleRequestQuery()
        : IRequest<OrganizerRoleRequestResponse?>;
}
