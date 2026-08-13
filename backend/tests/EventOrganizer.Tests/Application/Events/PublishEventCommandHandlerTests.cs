using EventOrganizer.Application.Commands.PublishEvent;
using EventOrganizer.Application.Common.Authorization;
using EventOrganizer.Application.Common.Constants;
using EventOrganizer.Application.Common.Exceptions;
using EventOrganizer.Application.Common.Interfaces;
using EventOrganizer.Domain.Bookings;
using EventOrganizer.Domain.Events;
using Microsoft.EntityFrameworkCore;

namespace EventOrganizer.Tests.Application.Events
{
    public sealed class PublishEventCommandHandlerTests : ApplicationTestBase
    {
        [Fact]
        public async Task Handle_WhenOrganizerOwnsEvent_PublishesEvent()
        {
            var organizerUserId = await CreateOrganizerUserAsync();
            var eventItem = await CreateEventAsync(organizerUserId);
            await CreateApprovedBookingAsync(eventItem);
            var handler = new PublishEventCommandHandler(
                DbContext,
                CreateAuthorizationService(organizerUserId, ApplicationRoles.Organizer));

            await handler.Handle(
                new PublishEventCommand(eventItem.Id),
                CancellationToken.None);

            var publishedEvent = await ReloadEventAsync(eventItem.Id);
            Assert.Equal(EventStatus.Published, publishedEvent.Status);
            Assert.NotNull(publishedEvent.UpdatedAtUtc);
        }

        [Fact]
        public async Task Handle_WhenAdminManagesAnotherOrganizersEvent_PublishesEvent()
        {
            var organizerUserId = await CreateOrganizerUserAsync();
            var adminUserId = await CreateOrganizerUserAsync("admin@example.com");
            var eventItem = await CreateEventAsync(organizerUserId);
            await CreateApprovedBookingAsync(eventItem);
            var handler = new PublishEventCommandHandler(
                DbContext,
                CreateAuthorizationService(adminUserId, ApplicationRoles.Admin));

            await handler.Handle(
                new PublishEventCommand(eventItem.Id),
                CancellationToken.None);

            Assert.Equal(EventStatus.Published, (await ReloadEventAsync(eventItem.Id)).Status);
        }

        [Fact]
        public async Task Handle_WhenBookingIsMissing_ThrowsConflictException()
        {
            var organizerUserId = await CreateOrganizerUserAsync();
            var eventItem = await CreateEventAsync(organizerUserId);
            var handler = new PublishEventCommandHandler(
                DbContext,
                CreateAuthorizationService(organizerUserId, ApplicationRoles.Organizer));

            await Assert.ThrowsAsync<ConflictException>(() =>
                handler.Handle(
                    new PublishEventCommand(eventItem.Id),
                    CancellationToken.None));
        }

        [Theory]
        [InlineData(EventResourceBookingStatus.Draft)]
        [InlineData(EventResourceBookingStatus.Submitted)]
        [InlineData(EventResourceBookingStatus.Rejected)]
        [InlineData(EventResourceBookingStatus.Expired)]
        public async Task Handle_WhenBookingIsNotApproved_ThrowsConflictException(
            EventResourceBookingStatus status)
        {
            var organizerUserId = await CreateOrganizerUserAsync();
            var eventItem = await CreateEventAsync(organizerUserId);
            var booking = await CreateBookingAsync(eventItem);
            if (status != EventResourceBookingStatus.Draft)
            {
                await SetBookingStatusAsync(booking.Id, status);
            }

            var handler = new PublishEventCommandHandler(
                DbContext,
                CreateAuthorizationService(organizerUserId, ApplicationRoles.Organizer));

            await Assert.ThrowsAsync<ConflictException>(() =>
                handler.Handle(
                    new PublishEventCommand(eventItem.Id),
                    CancellationToken.None));
        }

        [Fact]
        public async Task Handle_WhenEventStartIsInPast_ThrowsConflictException()
        {
            var organizerUserId = await CreateOrganizerUserAsync();
            var startsAtUtc = DateTime.UtcNow.AddHours(-2);
            var eventItem = await CreateEventAsync(
                organizerUserId,
                startsAtUtc: startsAtUtc,
                endsAtUtc: startsAtUtc.AddHours(1));
            await CreateApprovedBookingAsync(eventItem);
            var handler = new PublishEventCommandHandler(
                DbContext,
                CreateAuthorizationService(organizerUserId, ApplicationRoles.Organizer));

            await Assert.ThrowsAsync<ConflictException>(() =>
                handler.Handle(
                    new PublishEventCommand(eventItem.Id),
                    CancellationToken.None));
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

        private async Task CreateApprovedBookingAsync(
            EventOrganizer.Domain.Events.Event eventItem)
        {
            var booking = await CreateBookingAsync(eventItem);
            await SetBookingStatusAsync(booking.Id, EventResourceBookingStatus.Approved);
        }

        private async Task<EventOrganizer.Domain.Events.Event> ReloadEventAsync(Guid eventId)
        {
            DbContext.ChangeTracker.Clear();

            return await DbContext.Events.SingleAsync(eventItem => eventItem.Id == eventId);
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
