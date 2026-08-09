using EventOrganizer.Domain.Resources;

namespace EventOrganizer.Domain.Bookings
{
    public sealed class EventResourceBookingItem
    {
        private EventResourceBookingItem() { }

        private EventResourceBookingItem(
            Guid id,
            Guid bookingId,
            Guid resourceId,
            ResourceType resourceType)
        {
            Id = id;
            BookingId = bookingId;
            ResourceId = resourceId;
            ResourceType = resourceType;
        }

        public Guid Id { get; private set; }

        public Guid BookingId { get; private set; }

        public Guid ResourceId { get; private set; }

        public ResourceType ResourceType { get; private set; }

        internal static EventResourceBookingItem Create(
            Guid bookingId,
            Guid resourceId,
            ResourceType resourceType)
        {
            return new EventResourceBookingItem(
                Guid.NewGuid(),
                bookingId,
                resourceId,
                resourceType);
        }
    }
}
