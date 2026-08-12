using EventOrganizer.Application.Responses;
using EventOrganizer.Domain.Bookings;
using EventOrganizer.Domain.Resources;

namespace EventOrganizer.Application.Common.Mapping
{
    public static class EventResourceBookingResponseMapper
    {
        public static EventResourceBookingResponse ToResponse(
            EventResourceBooking booking,
            IReadOnlyCollection<Resource> resources)
        {
            ArgumentNullException.ThrowIfNull(booking);
            ArgumentNullException.ThrowIfNull(resources);

            var resourceById = resources.ToDictionary(resource => resource.Id);

            EventBookingResourceResponse? Map(ResourceType type)
            {
                var item = booking.Items.SingleOrDefault(item => item.ResourceType == type);
                return item is null ? null : ToResourceResponse(resourceById[item.ResourceId]);
            }

            var speakers = booking.Items
                .Where(item => item.ResourceType == ResourceType.Speaker)
                .Select(item => ToResourceResponse(resourceById[item.ResourceId]))
                .OrderBy(resource => resource.Name)
                .ToArray();

            var selectedResources = booking.Items
                .Select(item => resourceById[item.ResourceId])
                .ToArray();

            return new EventResourceBookingResponse(
                booking.Id,
                booking.EventId,
                booking.Status.ToString(),
                booking.Version,
                booking.SubmittedAtUtc,
                booking.HoldExpiresAtUtc,
                selectedResources.Sum(resource => resource.Cost),
                Map(ResourceType.Venue),
                speakers,
                Map(ResourceType.EquipmentPackage));
        }

        private static EventBookingResourceResponse ToResourceResponse(Resource resource)
        {
            return new EventBookingResourceResponse(
                resource.Id,
                resource.Name,
                resource.Type.ToString(),
                resource.Cost,
                resource.QualityScore);
        }
    }
}
