using MediatR;

namespace EventOrganizer.Application.Commands.MarkResourceAvailable
{
    public sealed record MarkResourceAvailableCommand(Guid ResourceId) : IRequest;
}
