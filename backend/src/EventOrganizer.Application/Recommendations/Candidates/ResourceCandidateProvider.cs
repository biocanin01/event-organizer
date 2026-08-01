using EventOrganizer.Application.Common.Interfaces;
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

            var candidates = await _dbContext.Resources
                .AsNoTracking()
                .Where(resource =>
                    resource.Status == ResourceStatus.Available
                    && (resource.Type == ResourceType.Venue
                        || resource.Type == ResourceType.Speaker
                        || resource.Type == ResourceType.Equipment))
                .Where(resource =>
                    resource.Type != ResourceType.Venue
                    || resource.Capacity >= eventItem.Capacity)
                .Where(resource => !_dbContext.ResourceReservations.Any(reservation =>
                    reservation.ResourceId == resource.Id
                    && reservation.StartsAtUtc < eventItem.EndsAtUtc
                    && reservation.EndsAtUtc > eventItem.StartsAtUtc
                    && (reservation.Status == ResourceReservationStatus.Pending
                        || reservation.Status == ResourceReservationStatus.Confirmed)))
                .OrderBy(resource => resource.Name)
                .Select(resource => new ResourceCandidate(
                    resource.Id,
                    resource.Name,
                    resource.Type,
                    resource.Cost,
                    resource.Capacity,
                    resource.Area,
                    resource.QualityScore))
                .ToListAsync(cancellationToken);

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
                .Where(candidate => candidate.Type == ResourceType.Equipment)
                .ToArray();

            return new ResourceCandidateSet(venues, speakers, equipment);
        }
    }
}
