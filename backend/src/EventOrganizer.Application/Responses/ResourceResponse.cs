namespace EventOrganizer.Application.Responses
{
    public sealed record ResourceResponse(
        Guid Id,
        string Name,
        string Description,
        string Type,
        string Status,
        DateTime CreatedAtUtc,
        DateTime? UpdatedAtUtc);
}
