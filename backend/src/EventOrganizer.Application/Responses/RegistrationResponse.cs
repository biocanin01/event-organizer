namespace EventOrganizer.Application.Responses
{
    public sealed record RegistrationResponse(
        Guid Id,
        Guid EventId,
        string EventTitle,
        Guid ParticipantUserId,
        string ParticipantFullName,
        string ParticipantEmail,
        string Status,
        string? RejectionReason,
        DateTime? DecidedAtUtc,
        Guid? DecidedByUserId,
        int Version,
        DateTime CreatedAtUtc,
        DateTime? UpdatedAtUtc);
}
