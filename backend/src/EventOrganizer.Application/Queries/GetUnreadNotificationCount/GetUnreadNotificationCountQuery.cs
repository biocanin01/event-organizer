using EventOrganizer.Application.Responses;
using MediatR;

namespace EventOrganizer.Application.Queries.GetUnreadNotificationCount
{
    public sealed record GetUnreadNotificationCountQuery
        : IRequest<UnreadNotificationCountResponse>;
}
