using MediatR;

namespace EventOrganizer.Application.Commands.UpdateEvent
{
    public sealed record UpdateEventCommand(
        Guid EventId,
        string Title,
        string Description,
        DateTime StartsAtUtc,
        DateTime EndsAtUtc,
        int Capacity,
        decimal Budget,
        string Area,
        int RequiredSpeakerCount,
        bool RequiresEquipment) : IRequest;
}
