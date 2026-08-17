namespace EventOrganizer.Application.Responses
{
    public sealed record NotificationResponse(
        Guid Id,
        string Type,
        string Title,
        string Message,
        string? RelatedEntityType,
        Guid? RelatedEntityId,
        bool IsRead,
        DateTime CreatedAtUtc,
        DateTime? ReadAtUtc);
}
