using EventOrganizer.Domain.Resources;
using MediatR;

namespace EventOrganizer.Application.Commands.CreateResource
{
    public sealed record CreateResourceCommand(
        string Name,
        string Description,
        ResourceType Type,
        decimal Cost,
        int? Capacity,
        string? Area,
        int QualityScore) : IRequest<Guid>;
}
