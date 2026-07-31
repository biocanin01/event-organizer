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
            decimal cost,
            int? capacity,
            string? area,
            int qualityScore,
            DateTime createdAtUtc)
        {
            Id = id;
            Name = name;
            Description = description;
            Type = type;
            Cost = cost;
            Capacity = capacity;
            Area = NormalizeArea(area);
            QualityScore = qualityScore;
            Status = ResourceStatus.Available;
            CreatedAtUtc = createdAtUtc;
        }

        public Guid Id { get; private set; }

        public string Name { get; private set; } = string.Empty;

        public string Description { get; private set; } = string.Empty;

        public ResourceType Type { get; private set; }

        public ResourceStatus Status { get; private set; }

        public decimal Cost { get; private set; }

        public int? Capacity { get; private set; }

        public string? Area { get; private set; }

        public int QualityScore { get; private set; }

        public DateTime CreatedAtUtc { get; private set; }

        public DateTime? UpdatedAtUtc { get; private set; }

        public static Resource Create(
            string name,
            string description,
            ResourceType type,
            decimal cost,
            int? capacity,
            string? area,
            int qualityScore,
            DateTime createdAtUtc)
        {
            ValidateName(name);
            ValidateCost(cost);
            ValidateCapacity(capacity);
            ValidateQualityScore(qualityScore);

            return new Resource(
                Guid.NewGuid(),
                name.Trim(),
                description.Trim(),
                type,
                cost,
                capacity,
                area,
                qualityScore,
                createdAtUtc);
        }

        public void UpdateDetails(
            string name,
            string description,
            decimal cost,
            int? capacity,
            string? area,
            int qualityScore,
            DateTime updatedAtUtc)
        {
            EnsureNotArchived();
            ValidateName(name);
            ValidateCost(cost);
            ValidateCapacity(capacity);
            ValidateQualityScore(qualityScore);

            Name = name.Trim();
            Description = description.Trim();
            Cost = cost;
            Capacity = capacity;
            Area = NormalizeArea(area);
            QualityScore = qualityScore;
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

        private static void ValidateCost(decimal cost)
        {
            if (cost < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(cost), "Resource cost cannot be negative.");
            }
        }

        private static void ValidateCapacity(int? capacity)
        {
            if (capacity.HasValue && capacity.Value <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(capacity), "Resource capacity must be positive.");
            }
        }

        private static void ValidateQualityScore(int qualityScore)
        {
            if (qualityScore is < 1 or > 5)
            {
                throw new ArgumentOutOfRangeException(nameof(qualityScore), "Resource quality score must be between 1 and 5.");
            }
        }

        private static string? NormalizeArea(string? area)
        {
            return string.IsNullOrWhiteSpace(area)
                ? null
                : area.Trim();
        }
    }
}
