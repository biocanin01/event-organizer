using EventOrganizer.Application.Common.Validation;
using EventOrganizer.Domain.Resources;
using MediatR;

namespace EventOrganizer.Application.Commands.CreateResource
{
    public sealed record CreateResourceCommand(
        string Name,
        string Description,
        ResourceType Type,
        decimal Cost,
        int QualityScore,
        int? Capacity,
        string? ExpertiseArea,
        string? ProviderName,
        int? SupportedCapacity,
        string? ServiceArea,
        bool? IncludesTechnicalSupport,
        string? ContentsSummary) : IRequest<Guid>, IResourceDetails;
}
