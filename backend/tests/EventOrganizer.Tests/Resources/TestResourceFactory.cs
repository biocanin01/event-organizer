using EventOrganizer.Domain.Resources;

namespace EventOrganizer.Tests
{
    internal static class TestResourceFactory
    {
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
            return type switch
            {
                ResourceType.Venue => Venue.Create(
                    name,
                    description,
                    cost,
                    capacity ?? 100,
                    qualityScore,
                    createdAtUtc),
                ResourceType.Speaker => Speaker.Create(
                    name,
                    description,
                    cost,
                    area ?? "General",
                    qualityScore,
                    createdAtUtc),
                ResourceType.EquipmentPackage => EquipmentPackage.Create(
                    name,
                    description,
                    cost,
                    "Test provider",
                    capacity ?? 100,
                    area ?? "General",
                    true,
                    description,
                    qualityScore,
                    createdAtUtc),
                _ => throw new ArgumentOutOfRangeException(nameof(type)),
            };
        }
    }
}
