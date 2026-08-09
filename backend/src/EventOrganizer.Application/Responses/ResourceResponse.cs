namespace EventOrganizer.Application.Responses
{
    public sealed record ResourceResponse(
        Guid Id,
        string Name,
        string Description,
        string Type,
        string Status,
        decimal Cost,
        int QualityScore,
        int Version,
        int? Capacity,
        string? ExpertiseArea,
        string? ProviderName,
        int? SupportedCapacity,
        string? ServiceArea,
        bool? IncludesTechnicalSupport,
        string? ContentsSummary,
        DateTime CreatedAtUtc,
        DateTime? UpdatedAtUtc);
}
