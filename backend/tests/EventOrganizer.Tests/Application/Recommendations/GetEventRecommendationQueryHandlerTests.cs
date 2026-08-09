using EventOrganizer.Application.Common.Authorization;
using EventOrganizer.Application.Common.Constants;
using EventOrganizer.Application.Common.Exceptions;
using EventOrganizer.Application.Common.Interfaces;
using EventOrganizer.Application.Queries.GetEventRecommendation;
using EventOrganizer.Application.Recommendations.Candidates;
using EventOrganizer.Application.Recommendations.Optimization;
using EventOrganizer.Domain.Resources;

namespace EventOrganizer.Tests.Application.Recommendations
{
    public sealed class GetEventRecommendationQueryHandlerTests : ApplicationTestBase
    {
        [Fact]
        public async Task Handle_WhenRecommendationIsFeasible_ReturnsSuccessfulRecommendation()
        {
            var organizerUserId = await CreateOrganizerUserAsync();
            var eventItem = await CreateEventAsync(organizerUserId);
            var venue = await CreateResourceAsync("Main Hall", ResourceType.Venue, 350m, 120, "IT", 5);
            var speaker = await CreateResourceAsync("Architecture Speaker", ResourceType.Speaker, 200m, null, "it", 5);
            var equipment = await CreateResourceAsync("Projector", ResourceType.EquipmentPackage, 100m, null, null, 3);
            var handler = CreateHandler(organizerUserId, ApplicationRoles.Organizer);

            var result = await handler.Handle(
                new GetEventRecommendationQuery(eventItem.Id),
                CancellationToken.None);

            Assert.True(result.IsSuccessful);
            Assert.Equal(venue.Id, result.Venue?.Id);
            Assert.Equal(speaker.Id, Assert.Single(result.Speakers).Id);
            Assert.Equal(equipment.Id, Assert.Single(result.Equipment).Id);
            Assert.Equal(650m, result.TotalCost);
            Assert.Equal(13, result.TotalQualityScore);
            Assert.Empty(result.FailureReasons);
        }

        [Fact]
        public async Task Handle_WhenNoFeasibleRecommendationExists_ReturnsFailureResponse()
        {
            var organizerUserId = await CreateOrganizerUserAsync();
            var eventItem = await CreateEventAsync(organizerUserId);
            await CreateResourceAsync("Main Hall", ResourceType.Venue, 900m, 120, "IT", 5);
            await CreateResourceAsync("Architecture Speaker", ResourceType.Speaker, 200m, null, "IT", 5);
            var handler = CreateHandler(organizerUserId, ApplicationRoles.Organizer);

            var result = await handler.Handle(
                new GetEventRecommendationQuery(eventItem.Id),
                CancellationToken.None);

            Assert.False(result.IsSuccessful);
            Assert.Null(result.Venue);
            Assert.Empty(result.Speakers);
            Assert.Empty(result.Equipment);
            Assert.Contains(
                "No feasible recommendation within event budget.",
                result.FailureReasons);
        }

        [Fact]
        public async Task Handle_WhenEventDoesNotExist_ThrowsNotFoundException()
        {
            var organizerUserId = await CreateOrganizerUserAsync();
            var handler = CreateHandler(organizerUserId, ApplicationRoles.Organizer);

            await Assert.ThrowsAsync<NotFoundException>(() =>
                handler.Handle(
                    new GetEventRecommendationQuery(Guid.NewGuid()),
                    CancellationToken.None));
        }

        [Fact]
        public async Task Handle_WhenOrganizerDoesNotOwnEvent_ThrowsForbiddenException()
        {
            var ownerUserId = await CreateOrganizerUserAsync();
            var otherOrganizerUserId = await CreateOrganizerUserAsync("other-organizer@example.com");
            var eventItem = await CreateEventAsync(ownerUserId);
            var handler = CreateHandler(otherOrganizerUserId, ApplicationRoles.Organizer);

            await Assert.ThrowsAsync<ForbiddenException>(() =>
                handler.Handle(
                    new GetEventRecommendationQuery(eventItem.Id),
                    CancellationToken.None));
        }

        private GetEventRecommendationQueryHandler CreateHandler(
            Guid? userId,
            params string[] roles)
        {
            return new GetEventRecommendationQueryHandler(
                DbContext,
                new EventAuthorizationService(new TestCurrentUserService(userId, roles)),
                new ResourceCandidateProvider(DbContext),
                new ConstraintRecommendationOptimizer());
        }

        private async Task<Resource> CreateResourceAsync(
            string name,
            ResourceType type,
            decimal cost,
            int? capacity,
            string? area,
            int qualityScore)
        {
            var resource = TestResourceFactory.Create(
                name,
                $"Description for {name}.",
                type,
                cost,
                capacity,
                area,
                qualityScore,
                DateTime.UtcNow);

            DbContext.Resources.Add(resource);
            await DbContext.SaveChangesAsync();

            return resource;
        }

        private sealed class TestCurrentUserService : ICurrentUserService
        {
            private readonly IReadOnlyCollection<string> _roles;

            public TestCurrentUserService(Guid? userId, params string[] roles)
            {
                UserId = userId;
                _roles = roles;
            }

            public Guid? UserId { get; }

            public string? Email => null;

            public bool IsAuthenticated => UserId is not null;

            public IReadOnlyCollection<string> Roles => _roles;

            public bool IsInRole(string role) => _roles.Contains(role);
        }
    }
}
