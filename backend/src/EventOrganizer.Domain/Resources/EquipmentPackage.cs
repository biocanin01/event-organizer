namespace EventOrganizer.Domain.Resources
{
    public sealed class EquipmentPackage : Resource
    {
        private EquipmentPackage() { }

        private EquipmentPackage(
            Guid id,
            string name,
            string description,
            decimal cost,
            string providerName,
            int supportedCapacity,
            string serviceArea,
            bool includesTechnicalSupport,
            string contentsSummary,
            int qualityScore,
            DateTime createdAtUtc)
            : base(
                id,
                name,
                description,
                ResourceType.EquipmentPackage,
                cost,
                qualityScore,
                createdAtUtc)
        {
            ProviderName = NormalizeProviderName(providerName);
            SupportedCapacity = ValidateSupportedCapacity(supportedCapacity);
            ServiceArea = NormalizeServiceArea(serviceArea);
            IncludesTechnicalSupport = includesTechnicalSupport;
            ContentsSummary = NormalizeContentsSummary(contentsSummary);
        }

        public string ProviderName { get; private set; } = string.Empty;

        public int SupportedCapacity { get; private set; }

        public string ServiceArea { get; private set; } = string.Empty;

        public bool IncludesTechnicalSupport { get; private set; }

        public string ContentsSummary { get; private set; } = string.Empty;

        public static EquipmentPackage Create(
            string name,
            string description,
            decimal cost,
            string providerName,
            int supportedCapacity,
            string serviceArea,
            bool includesTechnicalSupport,
            string contentsSummary,
            int qualityScore,
            DateTime createdAtUtc)
        {
            return new EquipmentPackage(
                Guid.NewGuid(),
                name,
                description,
                cost,
                providerName,
                supportedCapacity,
                serviceArea,
                includesTechnicalSupport,
                contentsSummary,
                qualityScore,
                createdAtUtc);
        }

        public void UpdateDetails(
            string name,
            string description,
            decimal cost,
            string providerName,
            int supportedCapacity,
            string serviceArea,
            bool includesTechnicalSupport,
            string contentsSummary,
            int qualityScore,
            DateTime updatedAtUtc)
        {
            var normalizedProviderName = NormalizeProviderName(providerName);
            var validatedSupportedCapacity = ValidateSupportedCapacity(supportedCapacity);
            var normalizedServiceArea = NormalizeServiceArea(serviceArea);
            var normalizedContentsSummary = NormalizeContentsSummary(contentsSummary);

            UpdateSharedDetails(name, description, cost, qualityScore, updatedAtUtc);
            ProviderName = normalizedProviderName;
            SupportedCapacity = validatedSupportedCapacity;
            ServiceArea = normalizedServiceArea;
            IncludesTechnicalSupport = includesTechnicalSupport;
            ContentsSummary = normalizedContentsSummary;
        }

        private static string NormalizeProviderName(string providerName)
        {
            return NormalizeRequired(
                providerName,
                nameof(providerName),
                "Equipment package provider name is required.");
        }

        private static int ValidateSupportedCapacity(int supportedCapacity)
        {
            return ValidatePositive(
                supportedCapacity,
                nameof(supportedCapacity),
                "Equipment package supported capacity must be positive.");
        }

        private static string NormalizeServiceArea(string serviceArea)
        {
            return NormalizeRequired(
                serviceArea,
                nameof(serviceArea),
                "Equipment package service area is required.");
        }

        private static string NormalizeContentsSummary(string contentsSummary)
        {
            return NormalizeRequired(
                contentsSummary,
                nameof(contentsSummary),
                "Equipment package contents summary is required.");
        }
    }
}
