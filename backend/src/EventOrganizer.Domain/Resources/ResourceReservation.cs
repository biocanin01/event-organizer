namespace EventOrganizer.Domain.Resources
{
    public sealed class ResourceReservation
    {
        private ResourceReservation() { }

        private ResourceReservation(
            Guid id,
            Guid eventId,
            Guid resourceId,
            DateTime startsAtUtc,
            DateTime endsAtUtc,
            DateTime createdAtUtc)
        {
            Id = id;
            EventId = eventId;
            ResourceId = resourceId;
            StartsAtUtc = startsAtUtc;
            EndsAtUtc = endsAtUtc;
            Status = ResourceReservationStatus.Pending;
            CreatedAtUtc = createdAtUtc;
        }

        public Guid Id { get; private set; }

        public Guid EventId { get; private set; }

        public Guid ResourceId { get; private set; }

        public DateTime StartsAtUtc { get; private set; }

        public DateTime EndsAtUtc { get; private set; }

        public ResourceReservationStatus Status { get; private set; }

        public DateTime CreatedAtUtc { get; private set; }

        public DateTime? UpdatedAtUtc { get; private set; }

        public static ResourceReservation Create(
            Guid eventId,
            Guid resourceId,
            DateTime startsAtUtc,
            DateTime endsAtUtc,
            DateTime createdAtUtc)
        {
            if (eventId == Guid.Empty)
            {
                throw new ArgumentException("Event id is required.", nameof(eventId));
            }

            if (resourceId == Guid.Empty)
            {
                throw new ArgumentException("Resource id is required.", nameof(resourceId));
            }

            if (endsAtUtc <= startsAtUtc)
            {
                throw new ArgumentException("Reservation end date must be after the start date.");
            }

            return new ResourceReservation(
                Guid.NewGuid(),
                eventId,
                resourceId,
                startsAtUtc,
                endsAtUtc,
                createdAtUtc);
        }

        public void Confirm(DateTime updatedAtUtc)
        {
            if (Status != ResourceReservationStatus.Pending)
            {
                throw new InvalidOperationException("Only pending reservations can be confirmed.");
            }

            Status = ResourceReservationStatus.Confirmed;
            UpdatedAtUtc = updatedAtUtc;
        }

        public void Reject(DateTime updatedAtUtc)
        {
            if (Status != ResourceReservationStatus.Pending)
            {
                throw new InvalidOperationException("Only pending reservations can be rejected.");
            }

            Status = ResourceReservationStatus.Rejected;
            UpdatedAtUtc = updatedAtUtc;
        }

        public void Cancel(DateTime updatedAtUtc)
        {
            if (Status is ResourceReservationStatus.Rejected or ResourceReservationStatus.Cancelled)
            {
                throw new InvalidOperationException("Rejected or cancelled reservations cannot be cancelled.");
            }

            Status = ResourceReservationStatus.Cancelled;
            UpdatedAtUtc = updatedAtUtc;
        }
    }
}
