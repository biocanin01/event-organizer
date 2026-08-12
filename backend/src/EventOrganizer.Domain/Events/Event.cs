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
            decimal budget,
            string area,
            int requiredSpeakerCount,
            bool requiresEquipment,
            Guid organizerUserId,
            DateTime createdAtUtc)
        {
            Id = id;
            Title = title;
            Description = description;
            StartsAtUtc = startsAtUtc;
            EndsAtUtc = endsAtUtc;
            Capacity = capacity;
            Budget = budget;
            Area = area;
            RequiredSpeakerCount = requiredSpeakerCount;
            RequiresEquipment = requiresEquipment;
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

        public decimal Budget { get; private set; }

        public string Area { get; private set; } = string.Empty;

        public int RequiredSpeakerCount { get; private set; }

        public bool RequiresEquipment { get; private set; }

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
            decimal budget,
            string area,
            int requiredSpeakerCount,
            Guid organizerUserId,
            DateTime createdAtUtc,
            bool requiresEquipment = false)
        {
            ValidateTitle(title);
            ValidateSchedule(startsAtUtc, endsAtUtc);
            ValidateCapacity(capacity);
            ValidateBudget(budget);
            ValidateArea(area);
            ValidateRequiredSpeakerCount(requiredSpeakerCount);

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
                budget,
                area.Trim(),
                requiredSpeakerCount,
                requiresEquipment,
                organizerUserId,
                createdAtUtc);
        }

        public void UpdateDetails(
            string title,
            string description,
            DateTime startsAtUtc,
            DateTime endsAtUtc,
            int capacity,
            decimal budget,
            string area,
            int requiredSpeakerCount,
            bool requiresEquipment,
            DateTime updatedAtUtc)
        {
            EnsureEditable();
            ValidateTitle(title);
            ValidateSchedule(startsAtUtc, endsAtUtc);
            ValidateCapacity(capacity);
            ValidateBudget(budget);
            ValidateArea(area);
            ValidateRequiredSpeakerCount(requiredSpeakerCount);

            Title = title.Trim();
            Description = description.Trim();
            StartsAtUtc = startsAtUtc;
            EndsAtUtc = endsAtUtc;
            Capacity = capacity;
            Budget = budget;
            Area = area.Trim();
            RequiredSpeakerCount = requiredSpeakerCount;
            RequiresEquipment = requiresEquipment;
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

        private static void ValidateBudget(decimal budget)
        {
            if (budget <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(budget), "Event budget must be positive.");
            }
        }

        private static void ValidateArea(string area)
        {
            if (string.IsNullOrWhiteSpace(area))
            {
                throw new ArgumentException("Event area is required.", nameof(area));
            }
        }

        private static void ValidateRequiredSpeakerCount(int requiredSpeakerCount)
        {
            if (requiredSpeakerCount <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(requiredSpeakerCount),
                    "Required speaker count must be positive.");
            }
        }
    }
}
