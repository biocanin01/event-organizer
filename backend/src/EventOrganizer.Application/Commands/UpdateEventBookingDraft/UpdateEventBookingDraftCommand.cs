using EventOrganizer.Application.Responses;
using MediatR;

namespace EventOrganizer.Application.Commands.UpdateEventBookingDraft
{
    public sealed record UpdateEventBookingDraftCommand(
        Guid EventId,
        int Version,
        Guid? VenueId,
        IReadOnlyList<Guid> SpeakerIds,
        Guid? EquipmentPackageId) : IRequest<EventResourceBookingResponse>;
}
