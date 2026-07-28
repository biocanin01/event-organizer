namespace EventOrganizer.Api.Contracts.ResourceReservations
{
    public sealed record CreateResourceReservationRequest(
        Guid EventId,
        Guid ResourceId,
        DateTime StartsAtUtc,
        DateTime EndsAtUtc);
}
