using EventOrganizer.Application.Commands.CancelEvent;
using EventOrganizer.Application.Common.Authorization;
using EventOrganizer.Application.Common.Constants;
using EventOrganizer.Application.Common.Exceptions;
using EventOrganizer.Application.Common.Interfaces;
using EventOrganizer.Domain.Bookings;
using EventOrganizer.Domain.Events;
using Microsoft.EntityFrameworkCore;

namespace EventOrganizer.Tests.Application.Events
{
    public sealed class CancelEventCommandHandlerTests : ApplicationTestBase
    {
        [Fact]
        public async Task Handle_WhenOrganizerOwnsEvent_CancelsEvent()
        {
            var organizerUserId = await CreateOrganizerUserAsync();
            var eventItem = await CreateEventAsync(organizerUserId);
            var handler = new CancelEventCommandHandler(
                DbContext,
                CreateAuthorizationService(organizerUserId, ApplicationRoles.Organizer));

            await handler.Handle(
                new CancelEventCommand(eventItem.Id),
                CancellationToken.None);

            Assert.Equal(EventStatus.Cancelled, eventItem.Status);
            Assert.NotNull(eventItem.UpdatedAtUtc);
        }

        [Fact]
        public async Task Handle_WhenAdminManagesAnotherOrganizersEvent_CancelsEvent()
        {
            var organizerUserId = await CreateOrganizerUserAsync();
            var adminUserId = await CreateOrganizerUserAsync("admin@example.com");
            var eventItem = await CreateEventAsync(organizerUserId);
            var handler = new CancelEventCommandHandler(
                DbContext,
                CreateAuthorizationService(adminUserId, ApplicationRoles.Admin));

            await handler.Handle(
                new CancelEventCommand(eventItem.Id),
                CancellationToken.None);

            Assert.Equal(EventStatus.Cancelled, eventItem.Status);
        }

        [Theory]
        [InlineData(EventResourceBookingStatus.Draft)]
        [InlineData(EventResourceBookingStatus.Submitted)]
        [InlineData(EventResourceBookingStatus.Approved)]
        public async Task Handle_WhenEventHasActiveBooking_CancelsBooking(
            EventResourceBookingStatus status)
        {
            var organizerUserId = await CreateOrganizerUserAsync();
            var eventItem = await CreateEventAsync(organizerUserId);
            var booking = await CreateBookingAsync(eventItem);
            if (status != EventResourceBookingStatus.Draft)
            {
                booking = await SetBookingStatusAsync(booking.Id, status);
            }

            var handler = new CancelEventCommandHandler(
                DbContext,
                CreateAuthorizationService(organizerUserId, ApplicationRoles.Organizer));

            await handler.Handle(
                new CancelEventCommand(eventItem.Id),
                CancellationToken.None);

            DbContext.ChangeTracker.Clear();

            var cancelledBooking = await DbContext.EventResourceBookings
                .SingleAsync(item => item.Id == booking.Id);

            Assert.Equal(EventResourceBookingStatus.Cancelled, cancelledBooking.Status);
            Assert.NotNull(cancelledBooking.UpdatedAtUtc);
        }

        [Theory]
        [InlineData(EventResourceBookingStatus.Rejected)]
        [InlineData(EventResourceBookingStatus.Expired)]
        [InlineData(EventResourceBookingStatus.Cancelled)]
        public async Task Handle_WhenEventHasTerminalBooking_DoesNotChangeBooking(
            EventResourceBookingStatus status)
        {
            var organizerUserId = await CreateOrganizerUserAsync();
            var eventItem = await CreateEventAsync(organizerUserId);
            var booking = await CreateBookingAsync(eventItem);
            booking = await SetBookingStatusAsync(booking.Id, status);
            var handler = new CancelEventCommandHandler(
                DbContext,
                CreateAuthorizationService(organizerUserId, ApplicationRoles.Organizer));

            await handler.Handle(
                new CancelEventCommand(eventItem.Id),
                CancellationToken.None);

            DbContext.ChangeTracker.Clear();

            var unchangedBooking = await DbContext.EventResourceBookings
                .SingleAsync(item => item.Id == booking.Id);

            Assert.Equal(status, unchangedBooking.Status);
        }

        [Fact]
        public async Task Handle_WhenOrganizerDoesNotOwnEvent_ThrowsForbiddenException()
        {
            var ownerUserId = await CreateOrganizerUserAsync();
            var otherOrganizerUserId = await CreateOrganizerUserAsync("other-organizer@example.com");
            var eventItem = await CreateEventAsync(ownerUserId);
            var handler = new CancelEventCommandHandler(
                DbContext,
                CreateAuthorizationService(otherOrganizerUserId, ApplicationRoles.Organizer));

            await Assert.ThrowsAsync<ForbiddenException>(() =>
                handler.Handle(
                    new CancelEventCommand(eventItem.Id),
                    CancellationToken.None));
        }

        [Fact]
        public async Task Handle_WhenParticipantTriesToCancelEvent_ThrowsForbiddenException()
        {
            var organizerUserId = await CreateOrganizerUserAsync();
            var participantUserId = await CreateOrganizerUserAsync("participant@example.com");
            var eventItem = await CreateEventAsync(organizerUserId);
            var handler = new CancelEventCommandHandler(
                DbContext,
                CreateAuthorizationService(participantUserId, ApplicationRoles.Participant));

            await Assert.ThrowsAsync<ForbiddenException>(() =>
                handler.Handle(
                    new CancelEventCommand(eventItem.Id),
                    CancellationToken.None));
        }

        [Fact]
        public async Task Handle_WhenUserIsNotAuthenticated_ThrowsUnauthorizedException()
        {
            var eventItem = await CreateEventAsync();
            var handler = new CancelEventCommandHandler(
                DbContext,
                CreateAuthorizationService(null));

            await Assert.ThrowsAsync<UnauthorizedException>(() =>
                handler.Handle(
                    new CancelEventCommand(eventItem.Id),
                    CancellationToken.None));
        }

        [Fact]
        public async Task Handle_WhenEventDoesNotExist_ThrowsNotFoundException()
        {
            var organizerUserId = await CreateOrganizerUserAsync();
            var handler = new CancelEventCommandHandler(
                DbContext,
                CreateAuthorizationService(organizerUserId, ApplicationRoles.Organizer));

            var act = () => handler.Handle(
                new CancelEventCommand(Guid.NewGuid()),
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
