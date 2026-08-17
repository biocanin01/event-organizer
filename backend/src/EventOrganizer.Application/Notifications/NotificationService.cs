using EventOrganizer.Application.Common.Interfaces;
using EventOrganizer.Domain.Notifications;

namespace EventOrganizer.Application.Notifications
{
    public sealed class NotificationService : INotificationService
    {
        private readonly IApplicationDbContext _dbContext;

        public NotificationService(IApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public void AddOrganizerRoleRequestApproved(
            Guid recipientUserId,
            Guid requestId,
            DateTime createdAtUtc)
        {
            Add(
                recipientUserId,
                NotificationType.OrganizerRoleRequestApproved,
                "Zahtev za Organizer ulogu je odobren",
                "Vaš zahtev za Organizer ulogu je odobren.",
                createdAtUtc,
                NotificationRelatedEntityType.OrganizerRoleRequest,
                requestId);
        }

        public void AddOrganizerRoleRequestRejected(
            Guid recipientUserId,
            Guid requestId,
            string decisionReason,
            DateTime createdAtUtc)
        {
            Add(
                recipientUserId,
                NotificationType.OrganizerRoleRequestRejected,
                "Zahtev za Organizer ulogu je odbijen",
                $"Vaš zahtev za Organizer ulogu je odbijen. Razlog: {decisionReason}",
                createdAtUtc,
                NotificationRelatedEntityType.OrganizerRoleRequest,
                requestId);
        }

        public void AddBookingApproved(
            Guid recipientUserId,
            Guid eventId,
            string eventTitle,
            DateTime createdAtUtc)
        {
            AddEventNotification(
                recipientUserId,
                NotificationType.BookingApproved,
                "Booking je odobren",
                $"Booking resursa za događaj \"{eventTitle}\" je odobren.",
                eventId,
                createdAtUtc);
        }

        public void AddBookingRejected(
            Guid recipientUserId,
            Guid eventId,
            string eventTitle,
            string? decisionReason,
            DateTime createdAtUtc)
        {
            var message = string.IsNullOrWhiteSpace(decisionReason)
                ? $"Booking resursa za događaj \"{eventTitle}\" je odbijen."
                : $"Booking resursa za događaj \"{eventTitle}\" je odbijen. Razlog: {decisionReason}";
            AddEventNotification(
                recipientUserId,
                NotificationType.BookingRejected,
                "Booking je odbijen",
                message,
                eventId,
                createdAtUtc);
        }

        public void AddBookingExpired(
            Guid recipientUserId,
            Guid eventId,
            string eventTitle,
            DateTime createdAtUtc)
        {
            AddEventNotification(
                recipientUserId,
                NotificationType.BookingExpired,
                "Rezervacija resursa je istekla",
                $"Privremena rezervacija resursa za događaj \"{eventTitle}\" je istekla.",
                eventId,
                createdAtUtc);
        }

        public void AddRegistrationConfirmed(
            Guid recipientUserId,
            Guid eventId,
            string eventTitle,
            DateTime createdAtUtc)
        {
            AddEventNotification(
                recipientUserId,
                NotificationType.RegistrationConfirmed,
                "Prijava je potvrđena",
                $"Vaša prijava za događaj \"{eventTitle}\" je potvrđena.",
                eventId,
                createdAtUtc);
        }

        public void AddRegistrationRejected(
            Guid recipientUserId,
            Guid eventId,
            string eventTitle,
            string rejectionReason,
            DateTime createdAtUtc)
        {
            AddEventNotification(
                recipientUserId,
                NotificationType.RegistrationRejected,
                "Prijava je odbijena",
                $"Vaša prijava za događaj \"{eventTitle}\" je odbijena. Razlog: {rejectionReason}",
                eventId,
                createdAtUtc);
        }

        public void AddRegistrationCancelled(
            Guid recipientUserId,
            Guid eventId,
            string eventTitle,
            DateTime createdAtUtc)
        {
            AddEventNotification(
                recipientUserId,
                NotificationType.RegistrationCancelled,
                "Prijava je otkazana",
                $"Učesnik je otkazao prijavu za događaj \"{eventTitle}\".",
                eventId,
                createdAtUtc);
        }

        public void AddEventCancelled(
            IEnumerable<Guid> recipientUserIds,
            Guid eventId,
            string eventTitle,
            DateTime createdAtUtc)
        {
            AddEventNotifications(
                recipientUserIds,
                NotificationType.EventCancelled,
                "Događaj je otkazan",
                $"Događaj \"{eventTitle}\" je otkazan.",
                eventId,
                createdAtUtc);
        }

        public void AddReviewAvailable(
            IEnumerable<Guid> recipientUserIds,
            Guid eventId,
            string eventTitle,
            DateTime createdAtUtc)
        {
            AddEventNotifications(
                recipientUserIds,
                NotificationType.ReviewAvailable,
                "Možete ostaviti recenziju",
                $"Događaj \"{eventTitle}\" je završen. Možete ostaviti recenziju.",
                eventId,
                createdAtUtc);
        }

        private void AddEventNotifications(
            IEnumerable<Guid> recipientUserIds,
            NotificationType type,
            string title,
            string message,
            Guid eventId,
            DateTime createdAtUtc)
        {
            foreach (var recipientUserId in recipientUserIds.Distinct())
            {
                AddEventNotification(
                    recipientUserId,
                    type,
                    title,
                    message,
                    eventId,
                    createdAtUtc);
            }
        }

        private void AddEventNotification(
            Guid recipientUserId,
            NotificationType type,
            string title,
            string message,
            Guid eventId,
            DateTime createdAtUtc)
        {
            Add(
                recipientUserId,
                type,
                title,
                message,
                createdAtUtc,
                NotificationRelatedEntityType.Event,
                eventId);
        }

        private void Add(
            Guid recipientUserId,
            NotificationType type,
            string title,
            string message,
            DateTime createdAtUtc,
            NotificationRelatedEntityType relatedEntityType,
            Guid relatedEntityId)
        {
            _dbContext.Notifications.Add(Notification.Create(
                recipientUserId,
                type,
                title,
                message,
                createdAtUtc,
                relatedEntityType,
                relatedEntityId));
        }
    }
}
