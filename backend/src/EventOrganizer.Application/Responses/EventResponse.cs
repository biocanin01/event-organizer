namespace EventOrganizer.Application.Responses
{
    public sealed record EventResponse(
        Guid Id,
        string Title,
        string Description,
        DateTime StartsAtUtc,
        DateTime EndsAtUtc,
        int Capacity,
        int ConfirmedRegistrationCount,
        decimal Budget,
        string Area,
        int RequiredSpeakerCount,
        bool RequiresEquipment,
        Guid OrganizerUserId,
        string Status,
        DateTime CreatedAtUtc,
        DateTime? UpdatedAtUtc);
}
