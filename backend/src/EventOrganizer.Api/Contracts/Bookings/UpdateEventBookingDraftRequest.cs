namespace EventOrganizer.Api.Contracts.Bookings
{
    public sealed record UpdateEventBookingDraftRequest(
        int Version,
        Guid? VenueId,
        IReadOnlyList<Guid> SpeakerIds,
        Guid? EquipmentPackageId);
}
