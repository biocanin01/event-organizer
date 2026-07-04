using MediatR;

namespace EventOrganizer.Application.Commands.CreateEvent
{
    public sealed record CreateEventCommand(
        string Title,
        string Description,
        DateTime StartsAtUtc,
        DateTime EndsAtUtc,
        int Capacity,
        Guid OrganizerUserId) : IRequest<Guid>;
}
