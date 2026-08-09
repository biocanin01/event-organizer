using EventOrganizer.Domain.Resources;

namespace EventOrganizer.Api.Contracts.Resources
{
    public sealed record UpdateResourceRequest(
        string Name,
        string Description,
        ResourceType Type,
        decimal Cost,
        int QualityScore,
        int? Capacity,
        string? ExpertiseArea,
        string? ProviderName,
        int? SupportedCapacity,
        string? ServiceArea,
        bool? IncludesTechnicalSupport,
        string? ContentsSummary);
}
