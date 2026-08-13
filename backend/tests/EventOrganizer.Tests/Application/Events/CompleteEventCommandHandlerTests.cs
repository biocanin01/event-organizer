using EventOrganizer.Application.Commands.CompleteEvent;
using EventOrganizer.Application.Common.Authorization;
using EventOrganizer.Application.Common.Constants;
using EventOrganizer.Application.Common.Exceptions;
using EventOrganizer.Application.Common.Interfaces;
using EventOrganizer.Domain.Bookings;
using EventOrganizer.Domain.Events;
using Microsoft.EntityFrameworkCore;

namespace EventOrganizer.Tests.Application.Events
{
    public sealed class CompleteEventCommandHandlerTests : ApplicationTestBase
    {
        [Fact]
        public async Task Handle_WhenPublishedEventHasEnded_CompletesEventWithoutChangingBooking()
        {
            var organizerUserId = await CreateOrganizerUserAsync();
            var startsAtUtc = DateTime.UtcNow.AddHours(-4);
            var eventItem = await CreateEventAsync(
                organizerUserId,
                startsAtUtc: startsAtUtc,
                endsAtUtc: startsAtUtc.AddHours(2));
            var booking = await CreateBookingAsync(eventItem);
            await SetBookingStatusAsync(booking.Id, EventResourceBookingStatus.Approved);
            eventItem = await ReloadEventAsync(eventItem.Id);
            eventItem.Publish(DateTime.UtcNow.AddHours(-1));
            await DbContext.SaveChangesAsync();
            var handler = CreateHandler(organizerUserId, ApplicationRoles.Organizer);

            await handler.Handle(
                new CompleteEventCommand(eventItem.Id),
                CancellationToken.None);

            Assert.Equal(EventStatus.Completed, eventItem.Status);
            DbContext.ChangeTracker.Clear();
            var unchangedBooking = await DbContext.EventResourceBookings
                .SingleAsync(storedBooking => storedBooking.Id == booking.Id);
            Assert.Equal(EventResourceBookingStatus.Approved, unchangedBooking.Status);
        }

        [Theory]
        [InlineData(EventStatus.Draft)]
        [InlineData(EventStatus.Cancelled)]
        [InlineData(EventStatus.Completed)]
        public async Task Handle_WhenEventIsNotPublished_ThrowsConflictException(
            EventStatus status)
        {
            var organizerUserId = await CreateOrganizerUserAsync();
            var startsAtUtc = DateTime.UtcNow.AddHours(-4);
            var eventItem = await CreateEventAsync(
                organizerUserId,
                startsAtUtc: startsAtUtc,
                endsAtUtc: startsAtUtc.AddHours(2));
            if (status == EventStatus.Cancelled)
            {
                eventItem.Cancel(DateTime.UtcNow);
            }
            else if (status == EventStatus.Completed)
            {
                eventItem.Publish(DateTime.UtcNow);
                eventItem.Complete(DateTime.UtcNow);
            }

            await DbContext.SaveChangesAsync();
            var handler = CreateHandler(organizerUserId, ApplicationRoles.Organizer);

            await Assert.ThrowsAsync<ConflictException>(() =>
                handler.Handle(new CompleteEventCommand(eventItem.Id), CancellationToken.None));
        }

        [Fact]
        public async Task Handle_WhenEventHasNotEnded_ThrowsConflictException()
        {
            var organizerUserId = await CreateOrganizerUserAsync();
            var startsAtUtc = DateTime.UtcNow.AddHours(-1);
            var eventItem = await CreateEventAsync(
                organizerUserId,
                startsAtUtc: startsAtUtc,
                endsAtUtc: DateTime.UtcNow.AddHours(1));
            eventItem.Publish(DateTime.UtcNow);
            await DbContext.SaveChangesAsync();
            var handler = CreateHandler(organizerUserId, ApplicationRoles.Organizer);

            await Assert.ThrowsAsync<ConflictException>(() =>
                handler.Handle(new CompleteEventCommand(eventItem.Id), CancellationToken.None));
        }

        [Fact]
        public async Task Handle_WhenOrganizerDoesNotOwnEvent_ThrowsForbiddenException()
        {
            var ownerUserId = await CreateOrganizerUserAsync();
            var otherOrganizerUserId = await CreateOrganizerUserAsync("other@example.com");
            var startsAtUtc = DateTime.UtcNow.AddHours(-4);
            var eventItem = await CreateEventAsync(
                ownerUserId,
                startsAtUtc: startsAtUtc,
                endsAtUtc: startsAtUtc.AddHours(2));
            eventItem.Publish(DateTime.UtcNow);
            await DbContext.SaveChangesAsync();
            var handler = CreateHandler(otherOrganizerUserId, ApplicationRoles.Organizer);

            await Assert.ThrowsAsync<ForbiddenException>(() =>
                handler.Handle(new CompleteEventCommand(eventItem.Id), CancellationToken.None));
        }

        private async Task<EventOrganizer.Domain.Events.Event> ReloadEventAsync(Guid eventId)
        {
            DbContext.ChangeTracker.Clear();

            return await DbContext.Events.SingleAsync(eventItem => eventItem.Id == eventId);
        }

        private CompleteEventCommandHandler CreateHandler(Guid userId, params string[] roles)
        {
            return new CompleteEventCommandHandler(
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
