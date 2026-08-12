using EventOrganizer.Application.Common.Exceptions;
using EventOrganizer.Domain.Bookings;

namespace EventOrganizer.Application.Bookings
{
    internal static class EventBookingVersionGuard
    {
        public static void EnsureExpectedVersion(
            EventResourceBooking booking,
            int expectedVersion)
        {
            if (booking.Version != expectedVersion)
            {
                throw new ConflictException(
                    "The booking has changed. Refresh it and try again.");
            }
        }
    }
}
