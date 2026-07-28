namespace EventOrganizer.Application.Responses
{
    public sealed record ResourceReservationResponse(
        Guid Id,
        Guid EventId,
        Guid ResourceId,
        DateTime StartsAtUtc,
        DateTime EndsAtUtc,
        string Status,
        DateTime CreatedAtUtc,
        DateTime? UpdatedAtUtc);
}
