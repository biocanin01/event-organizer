namespace EventOrganizer.Domain.Resources
{
    public abstract class Resource
    {
        private protected Resource() { }

        private protected Resource(
            Guid id,
            string name,
            string description,
            ResourceType type,
            decimal cost,
            int qualityScore,
            DateTime createdAtUtc)
        {
            Id = id;
            Name = NormalizeRequired(name, nameof(name), "Resource name is required.");
            Description = NormalizeDescription(description);
            Type = type;
            Cost = ValidateCost(cost);
            QualityScore = ValidateQualityScore(qualityScore);
            Status = ResourceStatus.Available;
            Version = 1;
            CreatedAtUtc = createdAtUtc;
        }

        public Guid Id { get; private set; }

        public string Name { get; private set; } = string.Empty;

        public string Description { get; private set; } = string.Empty;

        public ResourceType Type { get; private set; }

        public ResourceStatus Status { get; private set; }

        public decimal Cost { get; private set; }

        public int QualityScore { get; private set; }

        public int Version { get; private set; }

        public DateTime CreatedAtUtc { get; private set; }

        public DateTime? UpdatedAtUtc { get; private set; }

        public void MarkUnavailable(DateTime updatedAtUtc)
        {
            EnsureNotArchived();
            Status = ResourceStatus.Unavailable;
            Touch(updatedAtUtc);
        }

        public void MarkAvailable(DateTime updatedAtUtc)
        {
            EnsureNotArchived();
            Status = ResourceStatus.Available;
            Touch(updatedAtUtc);
        }

        public void Archive(DateTime updatedAtUtc)
        {
            Status = ResourceStatus.Archived;
            Touch(updatedAtUtc);
        }

        private protected void UpdateSharedDetails(
            string name,
            string description,
            decimal cost,
            int qualityScore,
            DateTime updatedAtUtc)
        {
            EnsureNotArchived();
            var normalizedName = NormalizeRequired(
                name,
                nameof(name),
                "Resource name is required.");
            var normalizedDescription = NormalizeDescription(description);
            var validatedCost = ValidateCost(cost);
            var validatedQualityScore = ValidateQualityScore(qualityScore);

            Name = normalizedName;
            Description = normalizedDescription;
            Cost = validatedCost;
            QualityScore = validatedQualityScore;
            Touch(updatedAtUtc);
        }

        private protected static int ValidatePositive(
            int value,
            string parameterName,
            string errorMessage)
        {
            if (value <= 0)
            {
                throw new ArgumentOutOfRangeException(parameterName, errorMessage);
            }

            return value;
        }

        private protected static string NormalizeRequired(
            string value,
            string parameterName,
            string errorMessage)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException(errorMessage, parameterName);
            }

            return value.Trim();
        }

        private void EnsureNotArchived()
        {
            if (Status == ResourceStatus.Archived)
            {
                throw new InvalidOperationException("Archived resources cannot be changed.");
            }
        }

        private void Touch(DateTime updatedAtUtc)
        {
            UpdatedAtUtc = updatedAtUtc;
            Version++;
        }

        private static decimal ValidateCost(decimal cost)
        {
            if (cost < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(cost), "Resource cost cannot be negative.");
            }

            return cost;
        }

        private static string NormalizeDescription(string description)
        {
            return description?.Trim()
                ?? throw new ArgumentNullException(nameof(description));
        }

        private static int ValidateQualityScore(int qualityScore)
        {
            if (qualityScore is < 1 or > 5)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(qualityScore),
                    "Resource quality score must be between 1 and 5.");
            }

            return qualityScore;
        }
    }
}
