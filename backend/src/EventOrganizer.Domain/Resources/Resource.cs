namespace EventOrganizer.Domain.Resources
{
    public sealed class Resource
    {
        private Resource() { }

        private Resource(
            Guid id,
            string name,
            string description,
            ResourceType type,
            DateTime createdAtUtc)
        {
            Id = id;
            Name = name;
            Description = description;
            Type = type;
            Status = ResourceStatus.Available;
            CreatedAtUtc = createdAtUtc;
        }

        public Guid Id { get; private set; }

        public string Name { get; private set; } = string.Empty;

        public string Description { get; private set; } = string.Empty;

        public ResourceType Type { get; private set; }

        public ResourceStatus Status { get; private set; }

        public DateTime CreatedAtUtc { get; private set; }

        public DateTime? UpdatedAtUtc { get; private set; }

        public static Resource Create(
            string name,
            string description,
            ResourceType type,
            DateTime createdAtUtc)
        {
            ValidateName(name);

            return new Resource(
                Guid.NewGuid(),
                name.Trim(),
                description.Trim(),
                type,
                createdAtUtc);
        }

        public void UpdateDetails(string name, string description, DateTime updatedAtUtc)
        {
            EnsureNotArchived();
            ValidateName(name);

            Name = name.Trim();
            Description = description.Trim();
            UpdatedAtUtc = updatedAtUtc;
        }

        public void MarkUnavailable(DateTime updatedAtUtc)
        {
            EnsureNotArchived();
            Status = ResourceStatus.Unavailable;
            UpdatedAtUtc = updatedAtUtc;
        }

        public void MarkAvailable(DateTime updatedAtUtc)
        {
            EnsureNotArchived();
            Status = ResourceStatus.Available;
            UpdatedAtUtc = updatedAtUtc;
        }

        public void Archive(DateTime updatedAtUtc)
        {
            Status = ResourceStatus.Archived;
            UpdatedAtUtc = updatedAtUtc;
        }

        private void EnsureNotArchived()
        {
            if (Status == ResourceStatus.Archived)
            {
                throw new InvalidOperationException("Archived resources cannot be changed.");
            }
        }

        private static void ValidateName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("Resource name is required.", nameof(name));
            }
        }
    }
}
