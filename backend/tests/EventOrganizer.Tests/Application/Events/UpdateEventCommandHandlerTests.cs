using EventOrganizer.Application.Commands.UpdateEvent;
using EventOrganizer.Application.Common.Authorization;
using EventOrganizer.Application.Common.Constants;
using EventOrganizer.Application.Common.Exceptions;
using EventOrganizer.Application.Common.Interfaces;
using EventOrganizer.Domain.Bookings;
using EventOrganizer.Domain.Events;

namespace EventOrganizer.Tests.Application.Events
{
    public sealed class UpdateEventCommandHandlerTests : ApplicationTestBase
    {
        [Fact]
        public async Task Handle_WhenEventAndBookingAreDraft_UpdatesEvent()
        {
            var organizerUserId = await CreateOrganizerUserAsync();
            var eventItem = await CreateEventAsync(organizerUserId);
            await CreateBookingAsync(eventItem);
            var startsAtUtc = DateTime.UtcNow.AddDays(20);
            var handler = CreateHandler(organizerUserId, ApplicationRoles.Organizer);

            await handler.Handle(
                new UpdateEventCommand(
                    eventItem.Id,
                    "Updated Event",
                    "Updated description.",
                    startsAtUtc,
                    startsAtUtc.AddHours(3),
                    120,
                    1500m,
                    "Finance",
                    2,
                    true),
                CancellationToken.None);

            Assert.Equal("Updated Event", eventItem.Title);
            Assert.Equal(120, eventItem.Capacity);
            Assert.Equal("Finance", eventItem.Area);
            Assert.Equal(2, eventItem.RequiredSpeakerCount);
            Assert.True(eventItem.RequiresEquipment);
            Assert.NotNull(eventItem.UpdatedAtUtc);
        }

        [Fact]
        public async Task Handle_WhenBookingIsNotDraft_ThrowsConflictException()
        {
            var organizerUserId = await CreateOrganizerUserAsync();
            var eventItem = await CreateEventAsync(organizerUserId);
            var booking = await CreateBookingAsync(eventItem);
            await SetBookingStatusAsync(booking.Id, EventResourceBookingStatus.Submitted);
            var handler = CreateHandler(organizerUserId, ApplicationRoles.Organizer);

            await Assert.ThrowsAsync<ConflictException>(() =>
                handler.Handle(CreateValidCommand(eventItem.Id), CancellationToken.None));
        }

        [Fact]
        public async Task Handle_WhenEventIsPublished_ThrowsConflictException()
        {
            var organizerUserId = await CreateOrganizerUserAsync();
            var eventItem = await CreateEventAsync(organizerUserId);
            await CreateBookingAsync(eventItem);
            eventItem.Publish(DateTime.UtcNow);
            await DbContext.SaveChangesAsync();
            var handler = CreateHandler(organizerUserId, ApplicationRoles.Organizer);

            await Assert.ThrowsAsync<ConflictException>(() =>
                handler.Handle(CreateValidCommand(eventItem.Id), CancellationToken.None));
        }

        [Fact]
        public async Task Handle_WhenOrganizerDoesNotOwnEvent_ThrowsForbiddenException()
        {
            var ownerUserId = await CreateOrganizerUserAsync();
            var otherOrganizerUserId = await CreateOrganizerUserAsync("other@example.com");
            var eventItem = await CreateEventAsync(ownerUserId);
            await CreateBookingAsync(eventItem);
            var handler = CreateHandler(otherOrganizerUserId, ApplicationRoles.Organizer);

            await Assert.ThrowsAsync<ForbiddenException>(() =>
                handler.Handle(CreateValidCommand(eventItem.Id), CancellationToken.None));
        }

        private static UpdateEventCommand CreateValidCommand(Guid eventId)
        {
            var startsAtUtc = DateTime.UtcNow.AddDays(20);
            return new UpdateEventCommand(
                eventId,
                "Updated Event",
                "Updated description.",
                startsAtUtc,
                startsAtUtc.AddHours(3),
                120,
                1500m,
                "Finance",
                2,
                false);
        }

        private UpdateEventCommandHandler CreateHandler(Guid userId, params string[] roles)
        {
            return new UpdateEventCommandHandler(
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
