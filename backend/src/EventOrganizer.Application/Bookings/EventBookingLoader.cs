using EventOrganizer.Application.Common.Exceptions;
using EventOrganizer.Application.Common.Interfaces;
using EventOrganizer.Domain.Bookings;
using EventOrganizer.Domain.Events;
using Microsoft.EntityFrameworkCore;

namespace EventOrganizer.Application.Bookings
{
    internal static class EventBookingLoader
    {
        public static async Task<(Event EventItem, EventResourceBooking Booking)> LoadTrackedAsync(
            IApplicationDbContext dbContext,
            Guid eventId,
            CancellationToken cancellationToken)
        {
            var eventItem = await dbContext.Events
                .FirstOrDefaultAsync(
                    eventItem => eventItem.Id == eventId,
                    cancellationToken);

            if (eventItem is null)
            {
                throw new NotFoundException(nameof(Event), eventId);
            }

            var booking = await dbContext.EventResourceBookings
                .Include(booking => booking.Items)
                .FirstOrDefaultAsync(
                    booking => booking.EventId == eventId,
                    cancellationToken);

            if (booking is null)
            {
                throw new NotFoundException(nameof(EventResourceBooking), eventId);
            }

            return (eventItem, booking);
        }
    }
}
