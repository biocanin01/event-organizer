using EventOrganizer.Application.Commands.CancelEvent;
using EventOrganizer.Application.Common.Authorization;
using EventOrganizer.Application.Common.Constants;
using EventOrganizer.Application.Common.Exceptions;
using EventOrganizer.Application.Common.Interfaces;
using EventOrganizer.Domain.Bookings;
using EventOrganizer.Domain.Events;
using EventOrganizer.Domain.Notifications;
using EventOrganizer.Domain.Registrations;
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
                CreateAuthorizationService(organizerUserId, ApplicationRoles.Organizer),
                CreateNotificationService());

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
                CreateAuthorizationService(adminUserId, ApplicationRoles.Admin),
                CreateNotificationService());

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
                CreateAuthorizationService(organizerUserId, ApplicationRoles.Organizer),
                CreateNotificationService());

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
                CreateAuthorizationService(organizerUserId, ApplicationRoles.Organizer),
                CreateNotificationService());

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
                CreateAuthorizationService(otherOrganizerUserId, ApplicationRoles.Organizer),
                CreateNotificationService());

            await Assert.ThrowsAsync<ForbiddenException>(() =>
                handler.Handle(
                    new CancelEventCommand(eventItem.Id),
                    CancellationToken.None));
        }

        [Theory]
        [InlineData(RegistrationStatus.Pending)]
        [InlineData(RegistrationStatus.Confirmed)]
        public async Task Handle_WhenEventHasActiveRegistrations_CancelsThem(
            RegistrationStatus status)
        {
            var organizerUserId = await CreateOrganizerUserAsync();
            var participantUserId = await CreateOrganizerUserAsync("registered-participant@example.com");
            var eventItem = await CreateEventAsync(organizerUserId);
            var registration = Registration.Create(eventItem.Id, participantUserId, DateTime.UtcNow);
            if (status == RegistrationStatus.Confirmed)
            {
                registration.Confirm(organizerUserId, DateTime.UtcNow);
            }

            DbContext.Registrations.Add(registration);
            await DbContext.SaveChangesAsync();
            var handler = new CancelEventCommandHandler(
                DbContext,
                CreateAuthorizationService(organizerUserId, ApplicationRoles.Organizer),
                CreateNotificationService());

            await handler.Handle(new CancelEventCommand(eventItem.Id), CancellationToken.None);

            Assert.Equal(RegistrationStatus.Cancelled, registration.Status);
            var notification = await DbContext.Notifications.SingleAsync();
            Assert.Equal(participantUserId, notification.RecipientUserId);
            Assert.Equal(NotificationType.EventCancelled, notification.Type);
            Assert.Equal(NotificationRelatedEntityType.Event, notification.RelatedEntityType);
            Assert.Equal(eventItem.Id, notification.RelatedEntityId);
        }

        [Fact]
        public async Task Handle_WhenParticipantTriesToCancelEvent_ThrowsForbiddenException()
        {
            var organizerUserId = await CreateOrganizerUserAsync();
            var participantUserId = await CreateOrganizerUserAsync("participant@example.com");
            var eventItem = await CreateEventAsync(organizerUserId);
            var handler = new CancelEventCommandHandler(
                DbContext,
                CreateAuthorizationService(participantUserId, ApplicationRoles.Participant),
                CreateNotificationService());

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
                CreateAuthorizationService(null),
                CreateNotificationService());

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
                CreateAuthorizationService(organizerUserId, ApplicationRoles.Organizer),
                CreateNotificationService());

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
