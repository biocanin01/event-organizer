using MediatR;

namespace EventOrganizer.Application.Commands.CompleteEvent
{
    public sealed record CompleteEventCommand(Guid EventId) : IRequest;
}
