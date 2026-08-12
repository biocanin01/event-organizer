using EventOrganizer.Application.Common.Interfaces;
using EventOrganizer.Application.Common.Mapping;
using EventOrganizer.Application.Responses;
using EventOrganizer.Domain.Bookings;
using EventOrganizer.Domain.Resources;
using Microsoft.EntityFrameworkCore;

namespace EventOrganizer.Application.Bookings
{
    internal static class EventBookingResponseFactory
    {
        public static async Task<EventResourceBookingResponse> CreateAsync(
            IApplicationDbContext dbContext,
            EventResourceBooking booking,
            CancellationToken cancellationToken)
        {
            var resourceIds = booking.Items
                .Select(item => item.ResourceId)
                .ToArray();

            var resources = resourceIds.Length == 0
                ? Array.Empty<Resource>()
                : await dbContext.Resources
                    .AsNoTracking()
                    .Where(resource => resourceIds.Contains(resource.Id))
                    .ToArrayAsync(cancellationToken);

            return EventResourceBookingResponseMapper.ToResponse(booking, resources);
        }
    }
}
