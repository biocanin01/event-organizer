using EventOrganizer.Domain.Resources;

namespace EventOrganizer.Api.Contracts.Resources
{
    public sealed record CreateResourceRequest(
        string Name,
        string Description,
        ResourceType Type,
        decimal Cost,
        int? Capacity,
        string? Area,
        int QualityScore);
}
