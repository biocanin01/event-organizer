namespace EventOrganizer.Application.Responses
{
    public sealed record ReviewResponse(
        Guid Id,
        Guid EventId,
        string EventTitle,
        Guid ParticipantUserId,
        string ParticipantName,
        int Rating,
        string Comment,
        int Version,
        DateTime CreatedAtUtc,
        DateTime? UpdatedAtUtc);
}
