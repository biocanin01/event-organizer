using EventOrganizer.Application.Responses;
using EventOrganizer.Domain.Users;
using MediatR;

namespace EventOrganizer.Application.Queries.ListOrganizerRoleRequests
{
    public sealed record ListOrganizerRoleRequestsQuery(OrganizerRoleRequestStatus? Status)
        : IRequest<IReadOnlyList<OrganizerRoleRequestResponse>>;
}
