namespace EventOrganizer.Api.Contracts.Resources
{
    public sealed record UpdateResourceRequest(
        string Name,
        string Description,
        decimal Cost,
        int? Capacity,
        string? Area,
        int QualityScore);
}
