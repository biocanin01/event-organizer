namespace EventOrganizer.Application.Responses
{
    public sealed record OrganizerRoleRequestResponse(
        Guid Id,
        Guid UserId,
        string Motivation,
        string Status,
        Guid? ReviewedByAdminUserId,
        string? DecisionReason,
        DateTime SubmittedAtUtc,
        DateTime? ReviewedAtUtc,
        DateTime? WithdrawnAtUtc,
        int Version);
}
