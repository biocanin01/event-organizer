using MediatR;

namespace EventOrganizer.Application.Commands.MarkNotificationAsRead
{
    public sealed record MarkNotificationAsReadCommand(Guid NotificationId) : IRequest;
}
