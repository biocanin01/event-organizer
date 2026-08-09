using EventOrganizer.Application.Responses;
using EventOrganizer.Domain.Resources;

namespace EventOrganizer.Application.Common.Mapping
{
    public static class ResourceResponseMapper
    {
        public static ResourceResponse ToResponse(Resource resource)
        {
            ArgumentNullException.ThrowIfNull(resource);

            var venue = resource as Venue;
            var speaker = resource as Speaker;
            var equipmentPackage = resource as EquipmentPackage;

            return new ResourceResponse(
                resource.Id,
                resource.Name,
                resource.Description,
                resource.Type.ToString(),
                resource.Status.ToString(),
                resource.Cost,
                resource.QualityScore,
                resource.Version,
                venue?.Capacity,
                speaker?.ExpertiseArea,
                equipmentPackage?.ProviderName,
                equipmentPackage?.SupportedCapacity,
                equipmentPackage?.ServiceArea,
                equipmentPackage?.IncludesTechnicalSupport,
                equipmentPackage?.ContentsSummary,
                resource.CreatedAtUtc,
                resource.UpdatedAtUtc);
        }
    }
}
