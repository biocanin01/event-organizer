namespace EventOrganizer.Application.Responses
{
    public sealed record ResourceResponse(
        Guid Id,
        string Name,
        string Description,
        string Type,
        string Status,
        decimal Cost,
        int? Capacity,
        string? Area,
        int QualityScore,
        DateTime CreatedAtUtc,
        DateTime? UpdatedAtUtc);
}
