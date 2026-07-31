namespace EventOrganizer.Application.Responses
{
    public sealed record EventResponse(
        Guid Id,
        string Title,
        string Description,
        DateTime StartsAtUtc,
        DateTime EndsAtUtc,
        int Capacity,
        decimal Budget,
        string Area,
        Guid OrganizerUserId,
        string Status,
        DateTime CreatedAtUtc,
        DateTime? UpdatedAtUtc);
}
