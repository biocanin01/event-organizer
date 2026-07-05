using MediatR;

namespace EventOrganizer.Application.Commands.CancelEvent
{
    public sealed record CancelEventCommand(Guid EventId) : IRequest;
}
