using EventOrganizer.Application.Common.Validation;
using MediatR;
using EventOrganizer.Domain.Resources;

namespace EventOrganizer.Application.Commands.UpdateResource
{
    public sealed record UpdateResourceCommand(
        Guid ResourceId,
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
        string? ContentsSummary) : IRequest, IResourceDetails;
}
