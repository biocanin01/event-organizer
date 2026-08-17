using EventOrganizer.Application.Responses;
using EventOrganizer.Domain.Notifications;

namespace EventOrganizer.Application.Notifications
{
    internal static class NotificationResponseMapper
    {
        public static NotificationResponse ToResponse(Notification notification)
        {
            return new NotificationResponse(
                notification.Id,
                notification.Type.ToString(),
                notification.Title,
                notification.Message,
                notification.RelatedEntityType?.ToString(),
                notification.RelatedEntityId,
                notification.IsRead,
                notification.CreatedAtUtc,
                notification.ReadAtUtc);
        }
    }
}
