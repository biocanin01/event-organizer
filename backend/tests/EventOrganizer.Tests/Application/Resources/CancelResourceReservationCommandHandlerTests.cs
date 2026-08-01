using EventOrganizer.Application.Commands.CancelResourceReservation;
using EventOrganizer.Application.Common.Authorization;
using EventOrganizer.Application.Common.Constants;
using EventOrganizer.Application.Common.Exceptions;
using EventOrganizer.Application.Common.Interfaces;
using EventOrganizer.Domain.Resources;
using Microsoft.EntityFrameworkCore;

namespace EventOrganizer.Tests.Application.Resources
{
    public sealed class CancelResourceReservationCommandHandlerTests : ApplicationTestBase
    {
        [Theory]
        [InlineData(ResourceReservationStatus.Pending)]
        [InlineData(ResourceReservationStatus.Confirmed)]
        public async Task Handle_WhenReservationCanBeCancelled_CancelsReservation(
            ResourceReservationStatus status)
        {
            var organizerUserId = await CreateOrganizerUserAsync();
            var reservation = await CreateReservationAsync(status, organizerUserId);
            var handler = new CancelResourceReservationCommandHandler(
                DbContext,
                CreateAuthorizationService(organizerUserId, ApplicationRoles.Organizer));

            await handler.Handle(
                new CancelResourceReservationCommand(reservation.Id),
                CancellationToken.None);

            DbContext.ChangeTracker.Clear();

            var cancelledReservation = await DbContext.ResourceReservations
                .SingleAsync(item => item.Id == reservation.Id);

            Assert.Equal(
                ResourceReservationStatus.Cancelled,
                cancelledReservation.Status);
            Assert.NotNull(cancelledReservation.UpdatedAtUtc);
        }

        [Fact]
        public async Task Handle_WhenReservationDoesNotExist_ThrowsNotFoundException()
        {
            var organizerUserId = Guid.NewGuid();
            var handler = new CancelResourceReservationCommandHandler(
                DbContext,
                CreateAuthorizationService(organizerUserId, ApplicationRoles.Organizer));

            var action = () => handler.Handle(
                new CancelResourceReservationCommand(Guid.NewGuid()),
                CancellationToken.None);

            await Assert.ThrowsAsync<NotFoundException>(action);
        }

        [Fact]
        public async Task Handle_WhenReservationIsRejected_ThrowsInvalidOperationException()
        {
            var organizerUserId = await CreateOrganizerUserAsync();
            var reservation = await CreateReservationAsync(
                ResourceReservationStatus.Rejected,
                organizerUserId);
            var handler = new CancelResourceReservationCommandHandler(
                DbContext,
                CreateAuthorizationService(organizerUserId, ApplicationRoles.Organizer));

            var action = () => handler.Handle(
                new CancelResourceReservationCommand(reservation.Id),
                CancellationToken.None);

            await Assert.ThrowsAsync<InvalidOperationException>(action);
        }

        [Fact]
        public async Task Handle_WhenAdminCancelsAnyReservation_CancelsReservation()
        {
            var organizerUserId = await CreateOrganizerUserAsync();
            var adminUserId = await CreateOrganizerUserAsync("admin@example.com");
            var reservation = await CreateReservationAsync(
                ResourceReservationStatus.Pending,
                organizerUserId);
            var handler = new CancelResourceReservationCommandHandler(
                DbContext,
                CreateAuthorizationService(adminUserId, ApplicationRoles.Admin));

            await handler.Handle(
                new CancelResourceReservationCommand(reservation.Id),
                CancellationToken.None);

            Assert.Equal(ResourceReservationStatus.Cancelled, reservation.Status);
        }

        [Fact]
        public async Task Handle_WhenOrganizerDoesNotOwnReservationEvent_ThrowsForbiddenException()
        {
            var ownerUserId = await CreateOrganizerUserAsync();
            var otherOrganizerUserId = await CreateOrganizerUserAsync("other-organizer@example.com");
            var reservation = await CreateReservationAsync(
                ResourceReservationStatus.Pending,
                ownerUserId);
            var handler = new CancelResourceReservationCommandHandler(
                DbContext,
                CreateAuthorizationService(otherOrganizerUserId, ApplicationRoles.Organizer));

            var action = () => handler.Handle(
                new CancelResourceReservationCommand(reservation.Id),
                CancellationToken.None);

            await Assert.ThrowsAsync<ForbiddenException>(action);
        }

        [Fact]
        public async Task Handle_WhenParticipantCancelsReservation_ThrowsForbiddenException()
        {
            var organizerUserId = await CreateOrganizerUserAsync();
            var participantUserId = await CreateOrganizerUserAsync("participant@example.com");
            var reservation = await CreateReservationAsync(
                ResourceReservationStatus.Pending,
                organizerUserId);
            var handler = new CancelResourceReservationCommandHandler(
                DbContext,
                CreateAuthorizationService(participantUserId, ApplicationRoles.Participant));

            var action = () => handler.Handle(
                new CancelResourceReservationCommand(reservation.Id),
                CancellationToken.None);

            await Assert.ThrowsAsync<ForbiddenException>(action);
        }

        [Fact]
        public async Task Handle_WhenUserIsNotAuthenticated_ThrowsUnauthorizedException()
        {
            var organizerUserId = await CreateOrganizerUserAsync();
            var reservation = await CreateReservationAsync(
                ResourceReservationStatus.Pending,
                organizerUserId);
            var handler = new CancelResourceReservationCommandHandler(
                DbContext,
                CreateAuthorizationService(null));

            var action = () => handler.Handle(
                new CancelResourceReservationCommand(reservation.Id),
                CancellationToken.None);

            await Assert.ThrowsAsync<UnauthorizedException>(action);
        }

        private async Task<ResourceReservation> CreateReservationAsync(
            ResourceReservationStatus status,
            Guid organizerUserId)
        {
            var eventItem = await CreateEventAsync(organizerUserId);
            var resource = Resource.Create(
                "Main Conference Hall",
                "A hall suitable for conferences.",
                ResourceType.Venue,
                500m,
                150,
                "IT",
                4,
                DateTime.UtcNow);

            var reservation = ResourceReservation.Create(
                eventItem.Id,
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

            DbContext.Resources.Add(resource);
            DbContext.ResourceReservations.Add(reservation);
            await DbContext.SaveChangesAsync();

            return reservation;
        }

        private static ResourceReservationAuthorizationService CreateAuthorizationService(
            Guid? userId,
            params string[] roles)
        {
            return new ResourceReservationAuthorizationService(
                new TestCurrentUserService(userId, roles));
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
