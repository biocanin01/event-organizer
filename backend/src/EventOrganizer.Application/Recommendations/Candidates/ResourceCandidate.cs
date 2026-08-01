using EventOrganizer.Domain.Resources;

namespace EventOrganizer.Application.Recommendations.Candidates
{
    public sealed record ResourceCandidate(
        Guid Id,
        string Name,
        ResourceType Type,
        decimal Cost,
        int? Capacity,
        string? Area,
        int QualityScore);
}
