namespace EventOrganizer.Application.Responses
{
    public sealed record EventResourceBookingResponse(
        Guid Id,
        Guid EventId,
        string Status,
        int Version,
        DateTime? SubmittedAtUtc,
        DateTime? HoldExpiresAtUtc,
        string? DecisionReason,
        DateTime? DecidedAtUtc,
        Guid? DecidedByUserId,
        decimal TotalCost,
        EventBookingResourceResponse? Venue,
        IReadOnlyList<EventBookingResourceResponse> Speakers,
        EventBookingResourceResponse? EquipmentPackage);
}
