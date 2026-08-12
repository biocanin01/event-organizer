namespace EventOrganizer.Application.Common.Bookings
{
    public sealed record BookingConflictDetail(
        Guid ResourceId,
        string ResourceName,
        Guid EventId,
        DateTime StartsAtUtc,
        DateTime EndsAtUtc);
}
