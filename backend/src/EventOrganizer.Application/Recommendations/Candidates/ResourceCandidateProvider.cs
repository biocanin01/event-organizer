using EventOrganizer.Application.Common.Interfaces;
using EventOrganizer.Domain.Bookings;
using EventOrganizer.Domain.Events;
using EventOrganizer.Domain.Resources;
using Microsoft.EntityFrameworkCore;

namespace EventOrganizer.Application.Recommendations.Candidates
{
    public sealed class ResourceCandidateProvider : IResourceCandidateProvider
    {
        private readonly IApplicationDbContext _dbContext;

        public ResourceCandidateProvider(IApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<ResourceCandidateSet> GetCandidatesAsync(
            Event eventItem,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(eventItem);

            var now = DateTime.UtcNow;

            var resources = await _dbContext.Resources
                .AsNoTracking()
                .Where(resource =>
                    resource.Status == ResourceStatus.Available
                    && (resource.Type == ResourceType.Venue
                        || resource.Type == ResourceType.Speaker
                        || resource.Type == ResourceType.EquipmentPackage))
                .Where(resource => !_dbContext.EventResourceBookings.Any(booking =>
                    (booking.Status == EventResourceBookingStatus.Approved
                        || (booking.Status == EventResourceBookingStatus.Submitted
                            && booking.HoldExpiresAtUtc > now))
                    && booking.Items.Any(item => item.ResourceId == resource.Id)
                    && _dbContext.Events.Any(bookedEvent =>
                        bookedEvent.Id == booking.EventId
                        && bookedEvent.StartsAtUtc < eventItem.EndsAtUtc
                        && bookedEvent.EndsAtUtc > eventItem.StartsAtUtc)))
                .OrderBy(resource => resource.Name)
                .ToListAsync(cancellationToken);

            var candidates = resources
                .Where(resource =>
                    resource is not Venue venue
                    || venue.Capacity >= eventItem.Capacity)
                .Select(resource => new ResourceCandidate(
                    resource.Id,
                    resource.Name,
                    resource.Type,
                    resource.Cost,
                    GetCapacity(resource),
                    GetArea(resource),
                    resource.QualityScore))
                .ToArray();

            var venues = candidates
                .Where(candidate => candidate.Type == ResourceType.Venue)
                .ToArray();

            var speakers = candidates
                .Where(candidate =>
                    candidate.Type == ResourceType.Speaker
                    && string.Equals(
                        candidate.Area,
                        eventItem.Area,
                        StringComparison.OrdinalIgnoreCase))
                .ToArray();

            var equipment = candidates
                .Where(candidate =>
                    candidate.Type == ResourceType.EquipmentPackage
                    && candidate.Capacity >= eventItem.Capacity
                    && string.Equals(
                        candidate.Area,
                        eventItem.Area,
                        StringComparison.OrdinalIgnoreCase))
                .ToArray();

            return new ResourceCandidateSet(venues, speakers, equipment);
        }

        private static int? GetCapacity(Resource resource)
        {
            return resource switch
            {
                Venue venue => venue.Capacity,
                EquipmentPackage equipmentPackage => equipmentPackage.SupportedCapacity,
                _ => null,
            };
        }

        private static string? GetArea(Resource resource)
        {
            return resource switch
            {
                Speaker speaker => speaker.ExpertiseArea,
                EquipmentPackage equipmentPackage => equipmentPackage.ServiceArea,
                _ => null,
            };
        }
    }
}
