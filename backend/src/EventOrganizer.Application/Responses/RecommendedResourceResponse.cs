namespace EventOrganizer.Application.Responses
{
    public sealed record RecommendedResourceResponse(
        Guid Id,
        string Name,
        string Type,
        decimal Cost,
        int? Capacity,
        string? Area,
        int QualityScore);
}
