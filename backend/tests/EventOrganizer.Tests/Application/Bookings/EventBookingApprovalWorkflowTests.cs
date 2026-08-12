using EventOrganizer.Application.Commands.ApproveEventBooking;
using EventOrganizer.Application.Commands.ExpireEventBookings;
using EventOrganizer.Application.Commands.RejectEventBooking;
using EventOrganizer.Application.Commands.ReviseEventBooking;
using EventOrganizer.Application.Commands.SubmitEventBooking;
using EventOrganizer.Application.Common.Authorization;
using EventOrganizer.Application.Common.Constants;
using EventOrganizer.Application.Common.Exceptions;
using EventOrganizer.Application.Common.Interfaces;
using EventOrganizer.Application.Common.Options;
using EventOrganizer.Domain.Bookings;
using EventOrganizer.Domain.Resources;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace EventOrganizer.Tests.Application.Bookings
{
    public sealed class EventBookingApprovalWorkflowTests : ApplicationTestBase
    {
        [Fact]
        public async Task Approve_SubmittedBookingWithActiveHold_ApprovesBooking()
        {
            var booking = await CreateSubmittedBookingAsync();
            var adminUserId = Guid.NewGuid();

            var response = await CreateApproveHandler(adminUserId).Handle(
                new ApproveEventBookingCommand(booking.Id, booking.Version),
                CancellationToken.None);

            Assert.Equal(EventResourceBookingStatus.Approved.ToString(), response.Status);
            Assert.Equal(adminUserId, response.DecidedByUserId);
            Assert.NotNull(response.DecidedAtUtc);
            Assert.Null(response.DecisionReason);
        }

        [Fact]
        public async Task Approve_WithExpiredHold_ThrowsConflictException()
        {
            var booking = await CreateSubmittedBookingAsync(
                holdExpiresAtUtc: DateTime.UtcNow.AddHours(-1));

            await Assert.ThrowsAsync<ConflictException>(() =>
                CreateApproveHandler(Guid.NewGuid()).Handle(
                    new ApproveEventBookingCommand(booking.Id, booking.Version),
                    CancellationToken.None));
        }

        [Fact]
        public async Task Reject_SubmittedBooking_StoresDecisionAndAllowsRevise()
        {
            var organizerUserId = await CreateOrganizerUserAsync();
            var eventItem = await CreateEventAsync(organizerUserId);
            var booking = await CreateSubmittedBookingAsync(eventItem);
            var adminUserId = Guid.NewGuid();

            var response = await CreateRejectHandler(adminUserId).Handle(
                new RejectEventBookingCommand(
                    booking.Id,
                    "  Speaker unavailable.  ",
                    booking.Version),
                CancellationToken.None);

            Assert.Equal(EventResourceBookingStatus.Rejected.ToString(), response.Status);
            Assert.Equal("Speaker unavailable.", response.DecisionReason);
            Assert.Equal(adminUserId, response.DecidedByUserId);

            var revised = await CreateReviseHandler(organizerUserId).Handle(
                new ReviseEventBookingCommand(eventItem.Id, response.Version),
                CancellationToken.None);

            Assert.Equal(booking.Id, revised.Id);
            Assert.Equal(EventResourceBookingStatus.Draft.ToString(), revised.Status);
            Assert.Null(revised.DecisionReason);
            Assert.Null(revised.DecidedByUserId);
        }

        [Fact]
        public async Task Expire_ExpiresOnlySubmittedBookingsWithExpiredHold()
        {
            var expiredBooking = await CreateSubmittedBookingAsync(
                holdExpiresAtUtc: DateTime.UtcNow.AddHours(-1));
            var activeBooking = await CreateSubmittedBookingAsync(
                eventTitle: "Active Submitted",
                startsAtUtc: DateTime.UtcNow.AddDays(20),
                holdExpiresAtUtc: DateTime.UtcNow.AddHours(1));
            var draftEvent = await CreateEventAsync(title: "Draft Event");
            var draftBooking = await CreateBookingAsync(draftEvent);

            var count = await CreateExpireHandler(Guid.NewGuid()).Handle(
                new ExpireEventBookingsCommand(),
                CancellationToken.None);

            Assert.Equal(1, count);
            Assert.Equal(
                EventResourceBookingStatus.Expired,
                (await ReloadBookingByIdAsync(expiredBooking.Id)).Status);
            Assert.Equal(
                EventResourceBookingStatus.Submitted,
                (await ReloadBookingByIdAsync(activeBooking.Id)).Status);
            Assert.Equal(
                EventResourceBookingStatus.Draft,
                (await ReloadBookingByIdAsync(draftBooking.Id)).Status);
        }

        [Fact]
        public async Task Approve_WithStaleVersion_ThrowsConflictException()
        {
            var booking = await CreateSubmittedBookingAsync();

            await Assert.ThrowsAsync<ConflictException>(() =>
                CreateApproveHandler(Guid.NewGuid()).Handle(
                    new ApproveEventBookingCommand(booking.Id, 999),
                    CancellationToken.None));
        }

        [Fact]
        public async Task Reject_WithOrganizerUser_ThrowsForbiddenException()
        {
            var booking = await CreateSubmittedBookingAsync();

            await Assert.ThrowsAsync<ForbiddenException>(() =>
                CreateRejectHandler(Guid.NewGuid(), ApplicationRoles.Organizer).Handle(
                    new RejectEventBookingCommand(booking.Id, null, booking.Version),
                    CancellationToken.None));
        }

        [Fact]
        public async Task Submit_WhenOtherBookingIsRejected_DoesNotConflict()
        {
            var organizerUserId = await CreateOrganizerUserAsync();
            var eventItem = await CreateEventAsync(organizerUserId);
            var venue = await CreateResourceAsync("Shared Venue", ResourceType.Venue, capacity: 120);
            var speaker = await CreateResourceAsync("Speaker", ResourceType.Speaker, area: "IT");
            var booking = await CreateBookingAsync(eventItem, venue, speaker);
            var otherEvent = await CreateEventAsync(
                title: "Rejected Event",
                startsAtUtc: eventItem.StartsAtUtc.AddHours(1));
            var otherBooking = await CreateBookingAsync(otherEvent, venue);
            await SetBookingStatusAsync(otherBooking.Id, EventResourceBookingStatus.Rejected);

            var response = await CreateSubmitHandler(organizerUserId).Handle(
                new SubmitEventBookingCommand(eventItem.Id, booking.Version),
                CancellationToken.None);

            Assert.Equal(EventResourceBookingStatus.Submitted.ToString(), response.Status);
        }

        [Fact]
        public async Task Submit_WhenOtherBookingIsExpired_DoesNotConflict()
        {
            var organizerUserId = await CreateOrganizerUserAsync();
            var eventItem = await CreateEventAsync(organizerUserId);
            var venue = await CreateResourceAsync("Shared Venue", ResourceType.Venue, capacity: 120);
            var speaker = await CreateResourceAsync("Speaker", ResourceType.Speaker, area: "IT");
            var booking = await CreateBookingAsync(eventItem, venue, speaker);
            var otherEvent = await CreateEventAsync(
                title: "Expired Event",
                startsAtUtc: eventItem.StartsAtUtc.AddHours(1));
            var otherBooking = await CreateBookingAsync(otherEvent, venue);
            await SetBookingStatusAsync(otherBooking.Id, EventResourceBookingStatus.Expired);

            var response = await CreateSubmitHandler(organizerUserId).Handle(
                new SubmitEventBookingCommand(eventItem.Id, booking.Version),
                CancellationToken.None);

            Assert.Equal(EventResourceBookingStatus.Submitted.ToString(), response.Status);
        }

        private async Task<EventResourceBooking> CreateSubmittedBookingAsync(
            EventOrganizer.Domain.Events.Event? eventItem = null,
            string eventTitle = "Submitted Event",
            DateTime? startsAtUtc = null,
            DateTime? holdExpiresAtUtc = null)
        {
            eventItem ??= await CreateEventAsync(
                title: eventTitle,
                startsAtUtc: startsAtUtc);
            var venue = await CreateResourceAsync(
                $"Venue {Guid.NewGuid():N}",
                ResourceType.Venue,
                capacity: 120);
            var speaker = await CreateResourceAsync(
                $"Speaker {Guid.NewGuid():N}",
                ResourceType.Speaker,
                area: "IT");
            var booking = await CreateBookingAsync(eventItem, venue, speaker);
            return await SetBookingStatusAsync(
                booking.Id,
                EventResourceBookingStatus.Submitted,
                holdExpiresAtUtc ?? DateTime.UtcNow.AddHours(1));
        }

        private async Task<EventResourceBooking> ReloadBookingByIdAsync(Guid bookingId)
        {
            DbContext.ChangeTracker.Clear();

            return await DbContext.EventResourceBookings
                .Include(booking => booking.Items)
                .SingleAsync(booking => booking.Id == bookingId);
        }

        private async Task<Resource> CreateResourceAsync(
            string name,
            ResourceType type,
            int? capacity = null,
            string? area = null)
        {
            var resource = TestResourceFactory.Create(
                name,
                $"Description for {name}.",
                type,
                100m,
                capacity,
                area,
                4,
                DateTime.UtcNow);

            DbContext.Resources.Add(resource);
            await DbContext.SaveChangesAsync();

            return resource;
        }

        private ApproveEventBookingCommandHandler CreateApproveHandler(
            Guid userId,
            params string[] roles)
        {
            DbContext.ChangeTracker.Clear();

            return new ApproveEventBookingCommandHandler(
                DbContext,
                new TestCurrentUserService(
                    userId,
                    roles.Length == 0 ? [ApplicationRoles.Admin] : roles));
        }

        private RejectEventBookingCommandHandler CreateRejectHandler(
            Guid userId,
            params string[] roles)
        {
            DbContext.ChangeTracker.Clear();

            return new RejectEventBookingCommandHandler(
                DbContext,
                new TestCurrentUserService(
                    userId,
                    roles.Length == 0 ? [ApplicationRoles.Admin] : roles));
        }

        private ExpireEventBookingsCommandHandler CreateExpireHandler(Guid userId)
        {
            DbContext.ChangeTracker.Clear();

            return new ExpireEventBookingsCommandHandler(
                DbContext,
                new TestCurrentUserService(userId, ApplicationRoles.Admin));
        }

        private ReviseEventBookingCommandHandler CreateReviseHandler(Guid userId)
        {
            DbContext.ChangeTracker.Clear();

            return new ReviseEventBookingCommandHandler(
                DbContext,
                new EventAuthorizationService(new TestCurrentUserService(
                    userId,
                    ApplicationRoles.Organizer)));
        }

        private SubmitEventBookingCommandHandler CreateSubmitHandler(Guid userId)
        {
            DbContext.ChangeTracker.Clear();

            return new SubmitEventBookingCommandHandler(
                DbContext,
                new EventAuthorizationService(new TestCurrentUserService(
                    userId,
                    ApplicationRoles.Organizer)),
                Options.Create(new BookingOptions
                {
                    HoldDurationHours = 48,
                }));
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
