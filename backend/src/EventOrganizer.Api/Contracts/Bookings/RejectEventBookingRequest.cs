namespace EventOrganizer.Api.Contracts.Bookings
{
    public sealed record RejectEventBookingRequest(
        int Version,
        string? Reason);
}
