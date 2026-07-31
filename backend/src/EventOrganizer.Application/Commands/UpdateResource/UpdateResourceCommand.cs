using MediatR;

namespace EventOrganizer.Application.Commands.UpdateResource
{
    public sealed record UpdateResourceCommand(
        Guid ResourceId,
        string Name,
        string Description,
        decimal Cost,
        int? Capacity,
        string? Area,
        int QualityScore) : IRequest;
}
