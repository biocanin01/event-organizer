using EventOrganizer.Application.Common.Exceptions;
using EventOrganizer.Domain.Bookings;
using EventOrganizer.Domain.Events;
using EventOrganizer.Domain.Resources;

namespace EventOrganizer.Application.Bookings
{
    internal static class BookingSubmissionValidator
    {
        public static void Validate(
            Event eventItem,
            EventResourceBooking booking,
            IReadOnlyCollection<Resource> resources)
        {
            var resourcesById = resources.ToDictionary(resource => resource.Id);

            foreach (var item in booking.Items)
            {
                if (!resourcesById.TryGetValue(item.ResourceId, out var resource))
                {
                    throw new NotFoundException(nameof(Resource), item.ResourceId);
                }

                if (resource.Type != item.ResourceType)
                {
                    throw new ConflictException(
                        $"Booking item '{item.ResourceId}' does not match the resource type.");
                }

                if (resource.Status != ResourceStatus.Available)
                {
                    throw new ConflictException($"Resource '{resource.Name}' is not available.");
                }
            }

            var venues = resources.OfType<Venue>().ToArray();
            if (venues.Length != 1)
            {
                throw new ConflictException("Booking submission requires exactly one venue.");
            }

            if (venues[0].Capacity < eventItem.Capacity)
            {
                throw new ConflictException("Selected venue does not support the event capacity.");
            }

            var speakers = resources.OfType<Speaker>().ToArray();
            if (speakers.Length != eventItem.RequiredSpeakerCount)
            {
                throw new ConflictException(
                    $"Booking submission requires exactly {eventItem.RequiredSpeakerCount} speaker resource(s).");
            }

            if (speakers.Any(speaker => !string.Equals(
                speaker.ExpertiseArea,
                eventItem.Area,
                StringComparison.OrdinalIgnoreCase)))
            {
                throw new ConflictException("All selected speakers must match the event area.");
            }

            var equipmentPackages = resources.OfType<EquipmentPackage>().ToArray();
            if (eventItem.RequiresEquipment && equipmentPackages.Length != 1)
            {
                throw new ConflictException(
                    "Booking submission requires exactly one equipment package.");
            }

            if (!eventItem.RequiresEquipment && equipmentPackages.Length != 0)
            {
                throw new ConflictException("This event does not require an equipment package.");
            }

            if (equipmentPackages.Length == 1)
            {
                var equipmentPackage = equipmentPackages[0];

                if (equipmentPackage.SupportedCapacity < eventItem.Capacity)
                {
                    throw new ConflictException(
                        "Selected equipment package does not support the event capacity.");
                }

                if (!string.Equals(
                    equipmentPackage.ServiceArea,
                    eventItem.Area,
                    StringComparison.OrdinalIgnoreCase))
                {
                    throw new ConflictException(
                        "Selected equipment package does not match the event area.");
                }
            }

            if (resources.Sum(resource => resource.Cost) > eventItem.Budget)
            {
                throw new ConflictException("Selected resources exceed the event budget.");
            }
        }
    }
}
