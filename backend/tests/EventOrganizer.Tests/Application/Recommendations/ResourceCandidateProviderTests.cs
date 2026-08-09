using EventOrganizer.Application.Recommendations.Candidates;
using EventOrganizer.Domain.Events;
using EventOrganizer.Domain.Resources;

namespace EventOrganizer.Tests.Application.Recommendations
{
    public sealed class ResourceCandidateProviderTests : ApplicationTestBase
    {
        [Fact]
        public async Task GetCandidatesAsync_ReturnsEligibleResourcesGroupedByType()
        {
            var eventItem = await CreateEventAsync();
            var venue = await CreateResourceAsync(
                "Main Hall",
                ResourceType.Venue,
                capacity: 120);
            var speaker = await CreateResourceAsync(
                "Architecture Speaker",
                ResourceType.Speaker,
                area: "it");
            var equipment = await CreateResourceAsync(
                "Projector",
                ResourceType.EquipmentPackage);

            var provider = new ResourceCandidateProvider(DbContext);

            var result = await provider.GetCandidatesAsync(
                eventItem,
                CancellationToken.None);

            Assert.Equal(venue.Id, Assert.Single(result.Venues).Id);
            Assert.Equal(speaker.Id, Assert.Single(result.Speakers).Id);
            Assert.Equal(equipment.Id, Assert.Single(result.Equipment).Id);
        }

        [Fact]
        public async Task GetCandidatesAsync_ExcludesResourcesThatDoNotMeetTypeConstraints()
        {
            var eventItem = await CreateEventAsync();
            await CreateResourceAsync(
                "Small Hall",
                ResourceType.Venue,
                capacity: 40);
            await CreateResourceAsync(
                "Medical Speaker",
                ResourceType.Speaker,
                area: "Medicine");

            var provider = new ResourceCandidateProvider(DbContext);

            var result = await provider.GetCandidatesAsync(
                eventItem,
                CancellationToken.None);

            Assert.Empty(result.Venues);
            Assert.Empty(result.Speakers);
        }

        [Fact]
        public async Task GetCandidatesAsync_ExcludesUnavailableAndArchivedResources()
        {
            var eventItem = await CreateEventAsync();
            var unavailable = await CreateResourceAsync(
                "Unavailable Projector",
                ResourceType.EquipmentPackage);
            var archived = await CreateResourceAsync(
                "Archived Projector",
                ResourceType.EquipmentPackage);

            unavailable.MarkUnavailable(DateTime.UtcNow);
            archived.Archive(DateTime.UtcNow);
            await DbContext.SaveChangesAsync();

            var provider = new ResourceCandidateProvider(DbContext);

            var result = await provider.GetCandidatesAsync(
                eventItem,
                CancellationToken.None);

            Assert.Empty(result.Equipment);
        }

        [Theory]
        [InlineData(ResourceReservationStatus.Pending, false)]
        [InlineData(ResourceReservationStatus.Confirmed, false)]
        [InlineData(ResourceReservationStatus.Rejected, true)]
        [InlineData(ResourceReservationStatus.Cancelled, true)]
        public async Task GetCandidatesAsync_UsesReservationStatusToDetermineAvailability(
            ResourceReservationStatus reservationStatus,
            bool shouldBeIncluded)
        {
            var eventItem = await CreateEventAsync();
            var equipment = await CreateResourceAsync(
                "Conference Projector",
                ResourceType.EquipmentPackage);
            await CreateReservationAsync(
                eventItem,
                equipment,
                reservationStatus,
                eventItem.StartsAtUtc.AddHours(1),
                eventItem.EndsAtUtc.AddHours(-1));

            var provider = new ResourceCandidateProvider(DbContext);

            var result = await provider.GetCandidatesAsync(
                eventItem,
                CancellationToken.None);

            Assert.Equal(
                shouldBeIncluded,
                result.Equipment.Any(candidate => candidate.Id == equipment.Id));
        }

        [Fact]
        public async Task GetCandidatesAsync_IncludesResourceWithNonOverlappingReservation()
        {
            var eventItem = await CreateEventAsync();
            var equipment = await CreateResourceAsync(
                "Available Projector",
                ResourceType.EquipmentPackage);
            await CreateReservationAsync(
                eventItem,
                equipment,
                ResourceReservationStatus.Confirmed,
                eventItem.StartsAtUtc.AddHours(-2),
                eventItem.StartsAtUtc);

            var provider = new ResourceCandidateProvider(DbContext);

            var result = await provider.GetCandidatesAsync(
                eventItem,
                CancellationToken.None);

            Assert.Equal(equipment.Id, Assert.Single(result.Equipment).Id);
        }

        private async Task<Resource> CreateResourceAsync(
            string name,
            ResourceType type,
            int? capacity = null,
            string? area = null)
        {
            var resource = TestResourceFactory.Create(
                name,
                $"Description for {name}.",
                type,
                100m,
                capacity,
                area,
                4,
                DateTime.UtcNow);

            DbContext.Resources.Add(resource);
            await DbContext.SaveChangesAsync();

            return resource;
        }

        private async Task CreateReservationAsync(
            Event eventItem,
            Resource resource,
            ResourceReservationStatus status,
            DateTime startsAtUtc,
            DateTime endsAtUtc)
        {
            var reservation = ResourceReservation.Create(
                eventItem.Id,
                resource.Id,
                startsAtUtc,
                endsAtUtc,
                DateTime.UtcNow);

            if (status == ResourceReservationStatus.Confirmed)
            {
                reservation.Confirm(DateTime.UtcNow);
            }
            else if (status == ResourceReservationStatus.Rejected)
            {
                reservation.Reject(DateTime.UtcNow);
            }
            else if (status == ResourceReservationStatus.Cancelled)
            {
                reservation.Cancel(DateTime.UtcNow);
            }

            DbContext.ResourceReservations.Add(reservation);
            await DbContext.SaveChangesAsync();
        }
    }
}
