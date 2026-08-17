namespace EventOrganizer.Application.Notifications
{
    public interface INotificationService
    {
        void AddOrganizerRoleRequestApproved(
            Guid recipientUserId,
            Guid requestId,
            DateTime createdAtUtc);

        void AddOrganizerRoleRequestRejected(
            Guid recipientUserId,
            Guid requestId,
            string decisionReason,
            DateTime createdAtUtc);

        void AddBookingApproved(
            Guid recipientUserId,
            Guid eventId,
            string eventTitle,
            DateTime createdAtUtc);

        void AddBookingRejected(
            Guid recipientUserId,
            Guid eventId,
            string eventTitle,
            string? decisionReason,
            DateTime createdAtUtc);

        void AddBookingExpired(
            Guid recipientUserId,
            Guid eventId,
            string eventTitle,
            DateTime createdAtUtc);

        void AddRegistrationConfirmed(
            Guid recipientUserId,
            Guid eventId,
            string eventTitle,
            DateTime createdAtUtc);

        void AddRegistrationRejected(
            Guid recipientUserId,
            Guid eventId,
            string eventTitle,
            string rejectionReason,
            DateTime createdAtUtc);

        void AddRegistrationCancelled(
            Guid recipientUserId,
            Guid eventId,
            string eventTitle,
            DateTime createdAtUtc);

        void AddEventCancelled(
            IEnumerable<Guid> recipientUserIds,
            Guid eventId,
            string eventTitle,
            DateTime createdAtUtc);

        void AddReviewAvailable(
            IEnumerable<Guid> recipientUserIds,
            Guid eventId,
            string eventTitle,
            DateTime createdAtUtc);
    }
}
