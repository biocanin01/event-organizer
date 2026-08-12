using EventOrganizer.Application.Common.Bookings;
using EventOrganizer.Application.Common.Interfaces;
using EventOrganizer.Domain.Bookings;
using EventOrganizer.Domain.Events;
using Microsoft.EntityFrameworkCore;

namespace EventOrganizer.Application.Bookings
{
    internal static class BookingConflictDetector
    {
        public static async Task<IReadOnlyList<BookingConflictDetail>> FindAsync(
            IApplicationDbContext dbContext,
            Event eventItem,
            EventResourceBooking booking,
            DateTime now,
            CancellationToken cancellationToken)
        {
            var resourceIds = booking.Items
                .Select(item => item.ResourceId)
                .ToArray();

            if (resourceIds.Length == 0)
            {
                return [];
            }

            var overlappingBookings = await dbContext.EventResourceBookings
                .AsNoTracking()
                .Include(otherBooking => otherBooking.Items)
                .Where(otherBooking =>
                    otherBooking.EventId != eventItem.Id
                    && (otherBooking.Status == EventResourceBookingStatus.Approved
                        || (otherBooking.Status == EventResourceBookingStatus.Submitted
                            && otherBooking.HoldExpiresAtUtc > now))
                    && otherBooking.Items.Any(item => resourceIds.Contains(item.ResourceId)))
                .Join(
                    dbContext.Events,
                    otherBooking => otherBooking.EventId,
                    otherEvent => otherEvent.Id,
                    (otherBooking, otherEvent) => new
                    {
                        Booking = otherBooking,
                        OtherEvent = otherEvent,
                    })
                .Where(candidate =>
                    candidate.OtherEvent.StartsAtUtc < eventItem.EndsAtUtc
                    && candidate.OtherEvent.EndsAtUtc > eventItem.StartsAtUtc)
                .ToArrayAsync(cancellationToken);

            var conflictingResourceIds = overlappingBookings
                .SelectMany(candidate => candidate.Booking.Items)
                .Where(item => resourceIds.Contains(item.ResourceId))
                .Select(item => item.ResourceId)
                .Distinct()
                .ToArray();

            var resourceNames = conflictingResourceIds.Length == 0
                ? new Dictionary<Guid, string>()
                : await dbContext.Resources
                    .AsNoTracking()
                    .Where(resource => conflictingResourceIds.Contains(resource.Id))
                    .ToDictionaryAsync(
                        resource => resource.Id,
                        resource => resource.Name,
                        cancellationToken);

            return overlappingBookings
                .SelectMany(candidate => candidate.Booking.Items
                    .Where(item => resourceIds.Contains(item.ResourceId))
                    .Select(item => new BookingConflictDetail(
                        item.ResourceId,
                        resourceNames[item.ResourceId],
                        candidate.OtherEvent.Id,
                        candidate.OtherEvent.StartsAtUtc,
                        candidate.OtherEvent.EndsAtUtc)))
                .ToArray();
        }
    }
}
