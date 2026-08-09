namespace EventOrganizer.Domain.Resources
{
    public sealed class Venue : Resource
    {
        private Venue() { }

        private Venue(
            Guid id,
            string name,
            string description,
            decimal cost,
            int capacity,
            int qualityScore,
            DateTime createdAtUtc)
            : base(
                id,
                name,
                description,
                ResourceType.Venue,
                cost,
                qualityScore,
                createdAtUtc)
        {
            Capacity = ValidatePositive(
                capacity,
                nameof(capacity),
                "Venue capacity must be positive.");
        }

        public int Capacity { get; private set; }

        public static Venue Create(
            string name,
            string description,
            decimal cost,
            int capacity,
            int qualityScore,
            DateTime createdAtUtc)
        {
            return new Venue(
                Guid.NewGuid(),
                name,
                description,
                cost,
                capacity,
                qualityScore,
                createdAtUtc);
        }

        public void UpdateDetails(
            string name,
            string description,
            decimal cost,
            int capacity,
            int qualityScore,
            DateTime updatedAtUtc)
        {
            var validatedCapacity = ValidatePositive(
                capacity,
                nameof(capacity),
                "Venue capacity must be positive.");

            UpdateSharedDetails(name, description, cost, qualityScore, updatedAtUtc);
            Capacity = validatedCapacity;
        }
    }
}
