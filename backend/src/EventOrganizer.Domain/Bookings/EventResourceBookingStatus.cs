namespace EventOrganizer.Domain.Bookings
{
    public enum EventResourceBookingStatus
    {
        Draft = 0,
        Submitted = 1,
        Approved = 2,
        Rejected = 3,
        Expired = 4,
        Cancelled = 5,
    }
}
