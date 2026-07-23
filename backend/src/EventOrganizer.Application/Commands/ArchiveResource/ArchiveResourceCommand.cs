using MediatR;

namespace EventOrganizer.Application.Commands.ArchiveResource
{
    public sealed record ArchiveResourceCommand(Guid ResourceId) : IRequest;
}
