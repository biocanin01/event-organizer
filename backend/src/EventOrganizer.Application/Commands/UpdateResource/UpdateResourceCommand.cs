using MediatR;

namespace EventOrganizer.Application.Commands.UpdateResource
{
    public sealed record UpdateResourceCommand(
        Guid ResourceId,
        string Name,
        string Description) : IRequest;
}
