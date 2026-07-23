using MediatR;

namespace EventOrganizer.Application.Commands.MarkResourceUnavailable
{
    public sealed record MarkResourceUnavailableCommand(Guid ResourceId) : IRequest;
}
