namespace EventOrganizer.Application.Responses
{
    public sealed record RecentReviewResponse(
        Guid Id,
        Guid ParticipantUserId,
        string ParticipantName,
        int Rating,
        string Comment,
        DateTime CreatedAtUtc,
        DateTime? UpdatedAtUtc);
}
