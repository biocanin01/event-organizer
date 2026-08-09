using EventOrganizer.Application.Commands.CancelEvent;
using EventOrganizer.Application.Common.Authorization;
using EventOrganizer.Application.Common.Constants;
using EventOrganizer.Application.Common.Exceptions;
using EventOrganizer.Application.Common.Interfaces;
using EventOrganizer.Domain.Events;
using EventOrganizer.Domain.Resources;
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
        [InlineData(ResourceReservationStatus.Pending)]
        [InlineData(ResourceReservationStatus.Confirmed)]
        public async Task Handle_WhenEventHasActiveReservations_CancelsReservations(
            ResourceReservationStatus status)
        {
            var organizerUserId = await CreateOrganizerUserAsync();
            var eventItem = await CreateEventAsync(organizerUserId);
            var reservation = await CreateReservationAsync(eventItem.Id, status);
            var handler = new CancelEventCommandHandler(
                DbContext,
                CreateAuthorizationService(organizerUserId, ApplicationRoles.Organizer));

            await handler.Handle(
                new CancelEventCommand(eventItem.Id),
                CancellationToken.None);

            DbContext.ChangeTracker.Clear();

            var cancelledReservation = await DbContext.ResourceReservations
                .SingleAsync(item => item.Id == reservation.Id);

            Assert.Equal(ResourceReservationStatus.Cancelled, cancelledReservation.Status);
            Assert.NotNull(cancelledReservation.UpdatedAtUtc);
        }

        [Theory]
        [InlineData(ResourceReservationStatus.Rejected)]
        [InlineData(ResourceReservationStatus.Cancelled)]
        public async Task Handle_WhenEventHasInactiveReservations_DoesNotChangeReservations(
            ResourceReservationStatus status)
        {
            var organizerUserId = await CreateOrganizerUserAsync();
            var eventItem = await CreateEventAsync(organizerUserId);
            var reservation = await CreateReservationAsync(eventItem.Id, status);
            var handler = new CancelEventCommandHandler(
                DbContext,
                CreateAuthorizationService(organizerUserId, ApplicationRoles.Organizer));

            await handler.Handle(
                new CancelEventCommand(eventItem.Id),
                CancellationToken.None);

            DbContext.ChangeTracker.Clear();

            var unchangedReservation = await DbContext.ResourceReservations
                .SingleAsync(item => item.Id == reservation.Id);

            Assert.Equal(status, unchangedReservation.Status);
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

        private async Task<ResourceReservation> CreateReservationAsync(
            Guid eventId,
            ResourceReservationStatus status)
        {
            var resource = TestResourceFactory.Create(
                "Main Conference Hall",
                "A hall suitable for conferences.",
                ResourceType.Venue,
                500m,
                150,
                "IT",
                4,
                DateTime.UtcNow);

            var reservation = ResourceReservation.Create(
                eventId,
                resource.Id,
                new DateTime(2026, 9, 1, 9, 0, 0, DateTimeKind.Utc),
                new DateTime(2026, 9, 1, 11, 0, 0, DateTimeKind.Utc),
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

            DbContext.Resources.Add(resource);
            DbContext.ResourceReservations.Add(reservation);
            await DbContext.SaveChangesAsync();

            return reservation;
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
