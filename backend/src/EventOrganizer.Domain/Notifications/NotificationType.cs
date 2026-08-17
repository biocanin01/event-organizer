namespace EventOrganizer.Domain.Notifications
{
    public enum NotificationType
    {
        OrganizerRoleRequestApproved = 1,
        OrganizerRoleRequestRejected = 2,
        BookingApproved = 3,
        BookingRejected = 4,
        BookingExpired = 5,
        RegistrationConfirmed = 6,
        RegistrationRejected = 7,
        RegistrationCancelled = 8,
        EventCancelled = 9,
        ReviewAvailable = 10,
    }
}
