using EventOrganizer.Domain.Resources;

namespace EventOrganizer.Domain.Bookings
{
    public sealed class EventResourceBooking
    {
        private readonly List<EventResourceBookingItem> _items = [];

        private EventResourceBooking() { }

        private EventResourceBooking(
            Guid id,
            Guid eventId,
            DateTime createdAtUtc)
        {
            Id = id;
            EventId = eventId;
            Status = EventResourceBookingStatus.Draft;
            Version = 1;
            CreatedAtUtc = createdAtUtc;
        }

        public Guid Id { get; private set; }

        public Guid EventId { get; private set; }

        public EventResourceBookingStatus Status { get; private set; }

        public int Version { get; private set; }

        public DateTime CreatedAtUtc { get; private set; }

        public DateTime? UpdatedAtUtc { get; private set; }

        public IReadOnlyCollection<EventResourceBookingItem> Items => _items.AsReadOnly();

        public static EventResourceBooking Create(
            Guid eventId,
            DateTime createdAtUtc)
        {
            if (eventId == Guid.Empty)
            {
                throw new ArgumentException("Event id is required.", nameof(eventId));
            }

            return new EventResourceBooking(
                Guid.NewGuid(),
                eventId,
                createdAtUtc);
        }

        public void AddResource(
            Guid resourceId,
            ResourceType resourceType,
            DateTime updatedAtUtc)
        {
            EnsureDraft();

            if (resourceId == Guid.Empty)
            {
                throw new ArgumentException("Resource id is required.", nameof(resourceId));
            }

            if (!Enum.IsDefined(resourceType))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(resourceType),
                    "Unsupported resource type.");
            }

            if (_items.Any(item => item.ResourceId == resourceId))
            {
                throw new InvalidOperationException("Resource is already part of this booking.");
            }

            if (resourceType == ResourceType.Venue &&
                _items.Any(item => item.ResourceType == ResourceType.Venue))
            {
                throw new InvalidOperationException("A booking can contain only one venue.");
            }

            if (resourceType == ResourceType.EquipmentPackage &&
                _items.Any(item => item.ResourceType == ResourceType.EquipmentPackage))
            {
                throw new InvalidOperationException(
                    "A booking can contain only one equipment package.");
            }

            _items.Add(EventResourceBookingItem.Create(Id, resourceId, resourceType));
            Touch(updatedAtUtc);
        }

        public void RemoveResource(Guid resourceId, DateTime updatedAtUtc)
        {
            EnsureDraft();

            if (resourceId == Guid.Empty)
            {
                throw new ArgumentException("Resource id is required.", nameof(resourceId));
            }

            var item = _items.FirstOrDefault(item => item.ResourceId == resourceId);
            if (item is null)
            {
                throw new InvalidOperationException("Resource is not part of this booking.");
            }

            _items.Remove(item);
            Touch(updatedAtUtc);
        }

        public void Cancel(DateTime updatedAtUtc)
        {
            if (Status is not (EventResourceBookingStatus.Draft
                or EventResourceBookingStatus.Submitted
                or EventResourceBookingStatus.Approved))
            {
                throw new InvalidOperationException("Only active bookings can be cancelled.");
            }

            Status = EventResourceBookingStatus.Cancelled;
            Touch(updatedAtUtc);
        }

        private void EnsureDraft()
        {
            if (Status != EventResourceBookingStatus.Draft)
            {
                throw new InvalidOperationException("Only draft bookings can be changed.");
            }
        }

        private void Touch(DateTime updatedAtUtc)
        {
            UpdatedAtUtc = updatedAtUtc;
            Version++;
        }
    }
}
