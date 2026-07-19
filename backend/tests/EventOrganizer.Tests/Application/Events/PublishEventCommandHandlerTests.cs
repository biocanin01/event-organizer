using EventOrganizer.Application.Commands.PublishEvent;
using EventOrganizer.Application.Common.Authorization;
using EventOrganizer.Application.Common.Constants;
using EventOrganizer.Application.Common.Exceptions;
using EventOrganizer.Application.Common.Interfaces;
using EventOrganizer.Domain.Events;

namespace EventOrganizer.Tests.Application.Events
{
    public sealed class PublishEventCommandHandlerTests : ApplicationTestBase
    {
        [Fact]
        public async Task Handle_WhenOrganizerOwnsEvent_PublishesEvent()
        {
            var organizerUserId = await CreateOrganizerUserAsync();
            var eventItem = await CreateEventAsync(organizerUserId);
            var handler = new PublishEventCommandHandler(
                DbContext,
                CreateAuthorizationService(organizerUserId, ApplicationRoles.Organizer));

            await handler.Handle(
                new PublishEventCommand(eventItem.Id),
                CancellationToken.None);

            Assert.Equal(EventStatus.Published, eventItem.Status);
            Assert.NotNull(eventItem.UpdatedAtUtc);
        }

        [Fact]
        public async Task Handle_WhenAdminManagesAnotherOrganizersEvent_PublishesEvent()
        {
            var organizerUserId = await CreateOrganizerUserAsync();
            var adminUserId = await CreateOrganizerUserAsync("admin@example.com");
            var eventItem = await CreateEventAsync(organizerUserId);
            var handler = new PublishEventCommandHandler(
                DbContext,
                CreateAuthorizationService(adminUserId, ApplicationRoles.Admin));

            await handler.Handle(
                new PublishEventCommand(eventItem.Id),
                CancellationToken.None);

            Assert.Equal(EventStatus.Published, eventItem.Status);
        }

        [Fact]
        public async Task Handle_WhenOrganizerDoesNotOwnEvent_ThrowsForbiddenException()
        {
            var ownerUserId = await CreateOrganizerUserAsync();
            var otherOrganizerUserId = await CreateOrganizerUserAsync("other-organizer@example.com");
            var eventItem = await CreateEventAsync(ownerUserId);
            var handler = new PublishEventCommandHandler(
                DbContext,
                CreateAuthorizationService(otherOrganizerUserId, ApplicationRoles.Organizer));

            await Assert.ThrowsAsync<ForbiddenException>(() =>
                handler.Handle(
                    new PublishEventCommand(eventItem.Id),
                    CancellationToken.None));
        }

        [Fact]
        public async Task Handle_WhenParticipantTriesToPublishEvent_ThrowsForbiddenException()
        {
            var organizerUserId = await CreateOrganizerUserAsync();
            var participantUserId = await CreateOrganizerUserAsync("participant@example.com");
            var eventItem = await CreateEventAsync(organizerUserId);
            var handler = new PublishEventCommandHandler(
                DbContext,
                CreateAuthorizationService(participantUserId, ApplicationRoles.Participant));

            await Assert.ThrowsAsync<ForbiddenException>(() =>
                handler.Handle(
                    new PublishEventCommand(eventItem.Id),
                    CancellationToken.None));
        }

        [Fact]
        public async Task Handle_WhenUserIsNotAuthenticated_ThrowsUnauthorizedException()
        {
            var eventItem = await CreateEventAsync();
            var handler = new PublishEventCommandHandler(
                DbContext,
                CreateAuthorizationService(null));

            await Assert.ThrowsAsync<UnauthorizedException>(() =>
                handler.Handle(
                    new PublishEventCommand(eventItem.Id),
                    CancellationToken.None));
        }

        [Fact]
        public async Task Handle_WhenEventDoesNotExist_ThrowsNotFoundException()
        {
            var organizerUserId = await CreateOrganizerUserAsync();
            var handler = new PublishEventCommandHandler(
                DbContext,
                CreateAuthorizationService(organizerUserId, ApplicationRoles.Organizer));

            var act = () => handler.Handle(
                new PublishEventCommand(Guid.NewGuid()),
                CancellationToken.None);

            await Assert.ThrowsAsync<NotFoundException>(act);
        }

        private static EventAuthorizationService CreateAuthorizationService(
            Guid? userId,
            params string[] roles)
        {
            return new EventAuthorizationService(new TestCurrentUserService(userId, roles));
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
