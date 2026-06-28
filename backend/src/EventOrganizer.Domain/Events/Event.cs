namespace EventOrganizer.Domain.Events
{
    public sealed class Event
    {
        private Event() { }

        private Event(
            Guid id,
            string title,
            string description,
            DateTime startsAtUtc,
            DateTime endsAtUtc,
            int capacity,
            Guid organizerUserId,
            DateTime createdAtUtc)
        {
            Id = id;
            Title = title;
            Description = description;
            StartsAtUtc = startsAtUtc;
            EndsAtUtc = endsAtUtc;
            Capacity = capacity;
            OrganizerUserId = organizerUserId;
            Status = EventStatus.Draft;
            CreatedAtUtc = createdAtUtc;
        }

        public Guid Id { get; private set; }

        public string Title { get; private set; } = string.Empty;

        public string Description { get; private set; } = string.Empty;

        public DateTime StartsAtUtc { get; private set; }

        public DateTime EndsAtUtc { get; private set; }

        public int Capacity { get; private set; }

        public Guid OrganizerUserId { get; private set; }

        public EventStatus Status { get; private set; }

        public DateTime CreatedAtUtc { get; private set; }

        public DateTime? UpdatedAtUtc { get; private set; }

        public static Event Create(
            string title,
            string description,
            DateTime startsAtUtc,
            DateTime endsAtUtc,
            int capacity,
            Guid organizerUserId,
            DateTime createdAtUtc)
        {
            ValidateTitle(title);
            ValidateSchedule(startsAtUtc, endsAtUtc);
            ValidateCapacity(capacity);

            if (organizerUserId == Guid.Empty)
            {
                throw new ArgumentException("Organizer user id is required.", nameof(organizerUserId));
            }

            return new Event(
                Guid.NewGuid(),
                title.Trim(),
                description.Trim(),
                startsAtUtc,
                endsAtUtc,
                capacity,
                organizerUserId,
                createdAtUtc);
        }

        public void UpdateDetails(
            string title,
            string description,
            DateTime startsAtUtc,
            DateTime endsAtUtc,
            int capacity,
            DateTime updatedAtUtc)
        {
            EnsureEditable();
            ValidateTitle(title);
            ValidateSchedule(startsAtUtc, endsAtUtc);
            ValidateCapacity(capacity);

            Title = title.Trim();
            Description = description.Trim();
            StartsAtUtc = startsAtUtc;
            EndsAtUtc = endsAtUtc;
            Capacity = capacity;
            UpdatedAtUtc = updatedAtUtc;
        }

        public void Publish(DateTime updatedAtUtc)
        {
            if (Status != EventStatus.Draft)
            {
                throw new InvalidOperationException("Only draft events can be published.");
            }

            Status = EventStatus.Published;
            UpdatedAtUtc = updatedAtUtc;
        }

        public void Cancel(DateTime updatedAtUtc)
        {
            if (Status is EventStatus.Cancelled or EventStatus.Completed)
            {
                throw new InvalidOperationException("Cancelled or completed events cannot be cancelled.");
            }

            Status = EventStatus.Cancelled;
            UpdatedAtUtc = updatedAtUtc;
        }

        public void Complete(DateTime updatedAtUtc)
        {
            if (Status != EventStatus.Published)
            {
                throw new InvalidOperationException("Only published events can be completed.");
            }

            Status = EventStatus.Completed;
            UpdatedAtUtc = updatedAtUtc;
        }

        private void EnsureEditable()
        {
            if (Status is EventStatus.Cancelled or EventStatus.Completed)
            {
                throw new InvalidOperationException("Cancelled or completed events cannot be edited.");
            }
        }

        private static void ValidateTitle(string title)
        {
            if (string.IsNullOrWhiteSpace(title))
            {
                throw new ArgumentException("Event title is required.", nameof(title));
            }
        }

        private static void ValidateSchedule(DateTime startsAtUtc, DateTime endsAtUtc)
        {
            if (endsAtUtc <= startsAtUtc)
            {
                throw new ArgumentException("Event end date must be after the start date.");
            }
        }

        private static void ValidateCapacity(int capacity)
        {
            if (capacity <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(capacity), "Event capacity must be positive.");
            }
        }
    }
}
