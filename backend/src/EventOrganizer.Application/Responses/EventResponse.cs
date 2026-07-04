namespace EventOrganizer.Application.Responses
{
    public sealed record EventResponse(
        Guid Id,
        string Title,
        string Description,
        DateTime StartsAtUtc,
        DateTime EndsAtUtc,
        int Capacity,
        Guid OrganizerUserId,
        string Status,
        DateTime CreatedAtUtc,
        DateTime? UpdatedAtUtc);
}
