using MediatR;

namespace EventOrganizer.Application.Commands.PublishEvent
{
    public sealed record PublishEventCommand(Guid EventId) : IRequest;
}
