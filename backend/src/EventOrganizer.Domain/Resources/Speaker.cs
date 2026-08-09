namespace EventOrganizer.Domain.Resources
{
    public sealed class Speaker : Resource
    {
        private Speaker() { }

        private Speaker(
            Guid id,
            string name,
            string description,
            decimal cost,
            string expertiseArea,
            int qualityScore,
            DateTime createdAtUtc)
            : base(
                id,
                name,
                description,
                ResourceType.Speaker,
                cost,
                qualityScore,
                createdAtUtc)
        {
            ExpertiseArea = NormalizeRequired(
                expertiseArea,
                nameof(expertiseArea),
                "Speaker expertise area is required.");
        }

        public string ExpertiseArea { get; private set; } = string.Empty;

        public static Speaker Create(
            string name,
            string description,
            decimal cost,
            string expertiseArea,
            int qualityScore,
            DateTime createdAtUtc)
        {
            return new Speaker(
                Guid.NewGuid(),
                name,
                description,
                cost,
                expertiseArea,
                qualityScore,
                createdAtUtc);
        }

        public void UpdateDetails(
            string name,
            string description,
            decimal cost,
            string expertiseArea,
            int qualityScore,
            DateTime updatedAtUtc)
        {
            var normalizedExpertiseArea = NormalizeRequired(
                expertiseArea,
                nameof(expertiseArea),
                "Speaker expertise area is required.");

            UpdateSharedDetails(name, description, cost, qualityScore, updatedAtUtc);
            ExpertiseArea = normalizedExpertiseArea;
        }
    }
}
