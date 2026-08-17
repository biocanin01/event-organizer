using MediatR;

namespace EventOrganizer.Application.Commands.MarkAllNotificationsAsRead
{
    public sealed record MarkAllNotificationsAsReadCommand : IRequest;
}
