using EventOrganizer.Application.Responses;
using MediatR;

namespace EventOrganizer.Application.Queries.ListMyNotifications
{
    public sealed record ListMyNotificationsQuery : IRequest<IReadOnlyList<NotificationResponse>>;
}
