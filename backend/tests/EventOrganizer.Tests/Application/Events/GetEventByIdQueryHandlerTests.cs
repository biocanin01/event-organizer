using EventOrganizer.Application.Common.Authorization;
using EventOrganizer.Application.Common.Constants;
using EventOrganizer.Application.Common.Exceptions;
using EventOrganizer.Application.Common.Interfaces;
using EventOrganizer.Application.Queries.GetEventById;

namespace EventOrganizer.Tests.Application.Events
{
    public sealed class GetEventByIdQueryHandlerTests : ApplicationTestBase
    {
        [Fact]
        public async Task Handle_WhenEventExistsAndOrganizerOwnsIt_ReturnsEvent()
        {
            var organizerUserId = await CreateOrganizerUserAsync();
            var eventItem = await CreateEventAsync(organizerUserId);
            var handler = CreateHandler(organizerUserId, ApplicationRoles.Organizer);

            var result = await handler.Handle(
                new GetEventByIdQuery(eventItem.Id),
                CancellationToken.None);

            Assert.NotNull(result);
            Assert.Equal(eventItem.Id, result.Id);
            Assert.Equal(eventItem.Title, result.Title);
            Assert.Equal(eventItem.RequiredSpeakerCount, result.RequiredSpeakerCount);
            Assert.Equal(eventItem.Status.ToString(), result.Status);
        }

        [Fact]
        public async Task Handle_WhenAdminRequestsEvent_ReturnsEvent()
        {
            var eventItem = await CreateEventAsync();
            var handler = CreateHandler(Guid.NewGuid(), ApplicationRoles.Admin);

            var result = await handler.Handle(
                new GetEventByIdQuery(eventItem.Id),
                CancellationToken.None);

            Assert.NotNull(result);
            Assert.Equal(eventItem.Id, result.Id);
        }

        [Fact]
        public async Task Handle_WhenOrganizerDoesNotOwnEvent_ThrowsForbiddenException()
        {
            var eventItem = await CreateEventAsync();
            var handler = CreateHandler(Guid.NewGuid(), ApplicationRoles.Organizer);

            await Assert.ThrowsAsync<ForbiddenException>(() =>
                handler.Handle(new GetEventByIdQuery(eventItem.Id), CancellationToken.None));
        }

        [Fact]
        public async Task Handle_WhenEventDoesNotExist_ReturnsNull()
        {
            var handler = CreateHandler(Guid.NewGuid(), ApplicationRoles.Admin);

            var result = await handler.Handle(
                new GetEventByIdQuery(Guid.NewGuid()),
                CancellationToken.None);

            Assert.Null(result);
        }

        private GetEventByIdQueryHandler CreateHandler(Guid userId, params string[] roles)
        {
            return new GetEventByIdQueryHandler(
                DbContext,
                new EventAuthorizationService(new TestCurrentUserService(userId, roles)));
        }

        private sealed class TestCurrentUserService : ICurrentUserService
        {
            public TestCurrentUserService(Guid userId, params string[] roles)
            {
                UserId = userId;
                Roles = roles;
            }

            public Guid? UserId { get; }

            public string? Email => null;

            public bool IsAuthenticated => true;

            public IReadOnlyCollection<string> Roles { get; }

            public bool IsInRole(string role)
            {
                return Roles.Contains(role);
            }
        }
    }
}
