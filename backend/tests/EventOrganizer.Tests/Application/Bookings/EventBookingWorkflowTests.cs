using EventOrganizer.Application.Commands.ReviseEventBooking;
using EventOrganizer.Application.Commands.SubmitEventBooking;
using EventOrganizer.Application.Commands.UpdateEventBookingDraft;
using EventOrganizer.Application.Commands.WithdrawEventBooking;
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
    public sealed class EventBookingWorkflowTests : ApplicationTestBase
    {
        [Fact]
        public async Task UpdateDraft_WithIncompleteSelection_SavesDraft()
        {
            var organizerUserId = await CreateOrganizerUserAsync();
            var eventItem = await CreateEventAsync(organizerUserId);
            var booking = await CreateBookingAsync(eventItem);
            var speaker = await CreateResourceAsync(
                "Architecture Speaker",
                ResourceType.Speaker,
                area: "IT");
            var handler = CreateUpdateHandler(organizerUserId);

            var response = await handler.Handle(
                new UpdateEventBookingDraftCommand(
                    eventItem.Id,
                    booking.Version,
                    null,
                    [speaker.Id],
                    null),
                CancellationToken.None);

            Assert.Equal(EventResourceBookingStatus.Draft.ToString(), response.Status);
            Assert.Equal(2, response.Version);
            Assert.Null(response.Venue);
            Assert.Equal(speaker.Id, Assert.Single(response.Speakers).Id);
            Assert.Null(response.EquipmentPackage);
        }

        [Fact]
        public async Task Submit_WithCompleteSelection_SubmitsAndSetsHold()
        {
            var organizerUserId = await CreateOrganizerUserAsync();
            var eventItem = await CreateEventAsync(organizerUserId);
            var (venue, speaker, equipmentPackage) = await CreateValidResourcesAsync();
            var booking = await CreateBookingAsync(eventItem);
            await UpdateDraftAsync(organizerUserId, eventItem.Id, booking.Version, venue.Id, [speaker.Id], null);
            booking = await ReloadBookingAsync(eventItem.Id);
            var handler = CreateSubmitHandler(organizerUserId);

            var response = await handler.Handle(
                new SubmitEventBookingCommand(eventItem.Id, booking.Version),
                CancellationToken.None);

            Assert.Equal(EventResourceBookingStatus.Submitted.ToString(), response.Status);
            Assert.NotNull(response.SubmittedAtUtc);
            Assert.NotNull(response.HoldExpiresAtUtc);
            Assert.Equal(venue.Id, response.Venue?.Id);
            Assert.Equal(speaker.Id, Assert.Single(response.Speakers).Id);
            Assert.Null(response.EquipmentPackage);
            Assert.Equal(venue.Cost + speaker.Cost, response.TotalCost);
            Assert.NotEqual(equipmentPackage.Id, response.EquipmentPackage?.Id);
        }

        [Fact]
        public async Task Submit_WhenEquipmentIsRequired_SubmitsWithEquipmentPackage()
        {
            var organizerUserId = await CreateOrganizerUserAsync();
            var eventItem = await CreateEventAsync(
                organizerUserId,
                requiresEquipment: true);
            var (venue, speaker, equipmentPackage) = await CreateValidResourcesAsync();
            var booking = await CreateBookingAsync(eventItem);
            await UpdateDraftAsync(
                organizerUserId,
                eventItem.Id,
                booking.Version,
                venue.Id,
                [speaker.Id],
                equipmentPackage.Id);
            booking = await ReloadBookingAsync(eventItem.Id);

            var response = await CreateSubmitHandler(organizerUserId).Handle(
                new SubmitEventBookingCommand(eventItem.Id, booking.Version),
                CancellationToken.None);

            Assert.Equal(equipmentPackage.Id, response.EquipmentPackage?.Id);
            Assert.Equal(
                venue.Cost + speaker.Cost + equipmentPackage.Cost,
                response.TotalCost);
        }

        [Fact]
        public async Task Submit_WhenRequiresEquipmentWithoutPackage_ThrowsConflictException()
        {
            var organizerUserId = await CreateOrganizerUserAsync();
            var eventItem = EventOrganizer.Domain.Events.Event.Create(
                "Software Architecture Seminar",
                "Seminar about modern web architecture.",
                DateTime.UtcNow.AddDays(10),
                DateTime.UtcNow.AddDays(10).AddHours(4),
                80,
                1000m,
                "IT",
                1,
                organizerUserId,
                DateTime.UtcNow,
                requiresEquipment: true);
            DbContext.Events.Add(eventItem);
            var booking = EventResourceBooking.Create(eventItem.Id, DateTime.UtcNow);
            DbContext.EventResourceBookings.Add(booking);
            await DbContext.SaveChangesAsync();
            var (venue, speaker, _) = await CreateValidResourcesAsync();
            await UpdateDraftAsync(organizerUserId, eventItem.Id, booking.Version, venue.Id, [speaker.Id], null);
            booking = await ReloadBookingAsync(eventItem.Id);
            var handler = CreateSubmitHandler(organizerUserId);

            var action = () => handler.Handle(
                new SubmitEventBookingCommand(eventItem.Id, booking.Version),
                CancellationToken.None);

            await Assert.ThrowsAsync<ConflictException>(action);
        }

        [Fact]
        public async Task Submit_WhenResourceIsHeldByOverlappingSubmittedBooking_ThrowsBookingConflictException()
        {
            var organizerUserId = await CreateOrganizerUserAsync();
            var eventItem = await CreateEventAsync(organizerUserId);
            var (venue, speaker, _) = await CreateValidResourcesAsync();
            var booking = await CreateBookingAsync(eventItem);
            await UpdateDraftAsync(organizerUserId, eventItem.Id, booking.Version, venue.Id, [speaker.Id], null);
            booking = await ReloadBookingAsync(eventItem.Id);
            var otherEvent = await CreateEventAsync(
                title: "Other Event",
                startsAtUtc: eventItem.StartsAtUtc.AddHours(1));
            var otherBooking = await CreateBookingAsync(otherEvent, venue);
            await SetBookingStatusAsync(
                otherBooking.Id,
                EventResourceBookingStatus.Submitted,
                DateTime.UtcNow.AddHours(1));
            var handler = CreateSubmitHandler(organizerUserId);

            var exception = await Assert.ThrowsAsync<BookingConflictException>(() =>
                handler.Handle(
                    new SubmitEventBookingCommand(eventItem.Id, booking.Version),
                    CancellationToken.None));

            var conflict = Assert.Single(exception.Conflicts);
            Assert.Equal(venue.Id, conflict.ResourceId);
            Assert.Equal(otherEvent.Id, conflict.EventId);
        }

        [Fact]
        public async Task Submit_WhenSubmittedHoldExpired_DoesNotConflict()
        {
            var organizerUserId = await CreateOrganizerUserAsync();
            var eventItem = await CreateEventAsync(organizerUserId);
            var (venue, speaker, _) = await CreateValidResourcesAsync();
            var booking = await CreateBookingAsync(eventItem);
            await UpdateDraftAsync(organizerUserId, eventItem.Id, booking.Version, venue.Id, [speaker.Id], null);
            booking = await ReloadBookingAsync(eventItem.Id);
            var otherEvent = await CreateEventAsync(
                title: "Other Event",
                startsAtUtc: eventItem.StartsAtUtc.AddHours(1));
            var otherBooking = await CreateBookingAsync(otherEvent, venue);
            await SetBookingStatusAsync(
                otherBooking.Id,
                EventResourceBookingStatus.Submitted,
                DateTime.UtcNow.AddHours(-1));
            var handler = CreateSubmitHandler(organizerUserId);

            var response = await handler.Handle(
                new SubmitEventBookingCommand(eventItem.Id, booking.Version),
                CancellationToken.None);

            Assert.Equal(EventResourceBookingStatus.Submitted.ToString(), response.Status);
        }

        [Theory]
        [InlineData(-1, 3, true)]
        [InlineData(3, 7, true)]
        [InlineData(1, 2, true)]
        [InlineData(-1, 5, true)]
        [InlineData(-4, 0, false)]
        [InlineData(4, 8, false)]
        public async Task Submit_DetectsOnlyOverlappingSchedules(
            int otherStartOffsetHours,
            int otherEndOffsetHours,
            bool shouldConflict)
        {
            var organizerUserId = await CreateOrganizerUserAsync();
            var eventItem = await CreateEventAsync(organizerUserId);
            var (venue, speaker, _) = await CreateValidResourcesAsync();
            var booking = await CreateBookingAsync(eventItem);
            await UpdateDraftAsync(
                organizerUserId,
                eventItem.Id,
                booking.Version,
                venue.Id,
                [speaker.Id],
                null);
            booking = await ReloadBookingAsync(eventItem.Id);
            var otherEvent = await CreateEventAsync(
                organizerUserId,
                $"Other Event {Guid.NewGuid():N}",
                eventItem.StartsAtUtc.AddHours(otherStartOffsetHours),
                eventItem.StartsAtUtc.AddHours(otherEndOffsetHours));
            var otherBooking = await CreateBookingAsync(otherEvent, venue);
            await SetBookingStatusAsync(
                otherBooking.Id,
                EventResourceBookingStatus.Submitted,
                DateTime.UtcNow.AddHours(1));
            var handler = CreateSubmitHandler(organizerUserId);

            var action = () => handler.Handle(
                new SubmitEventBookingCommand(eventItem.Id, booking.Version),
                CancellationToken.None);

            if (shouldConflict)
            {
                await Assert.ThrowsAsync<BookingConflictException>(action);
            }
            else
            {
                var response = await action();
                Assert.Equal(EventResourceBookingStatus.Submitted.ToString(), response.Status);
            }
        }

        [Theory]
        [InlineData(EventResourceBookingStatus.Approved, true)]
        [InlineData(EventResourceBookingStatus.Draft, false)]
        public async Task Submit_UsesOnlyBlockingBookingStatuses(
            EventResourceBookingStatus otherStatus,
            bool shouldConflict)
        {
            var organizerUserId = await CreateOrganizerUserAsync();
            var eventItem = await CreateEventAsync(organizerUserId);
            var (venue, speaker, _) = await CreateValidResourcesAsync();
            var booking = await CreateBookingAsync(eventItem);
            await UpdateDraftAsync(
                organizerUserId,
                eventItem.Id,
                booking.Version,
                venue.Id,
                [speaker.Id],
                null);
            booking = await ReloadBookingAsync(eventItem.Id);
            var otherEvent = await CreateEventAsync(
                organizerUserId,
                $"Other Event {Guid.NewGuid():N}",
                eventItem.StartsAtUtc.AddHours(1));
            var otherBooking = await CreateBookingAsync(otherEvent, venue);
            await SetBookingStatusAsync(otherBooking.Id, otherStatus);
            var handler = CreateSubmitHandler(organizerUserId);

            var action = () => handler.Handle(
                new SubmitEventBookingCommand(eventItem.Id, booking.Version),
                CancellationToken.None);

            if (shouldConflict)
            {
                await Assert.ThrowsAsync<BookingConflictException>(action);
            }
            else
            {
                var response = await action();
                Assert.Equal(EventResourceBookingStatus.Submitted.ToString(), response.Status);
            }
        }

        [Theory]
        [InlineData("missing-venue")]
        [InlineData("speaker-count")]
        [InlineData("unavailable")]
        [InlineData("venue-capacity")]
        [InlineData("speaker-area")]
        [InlineData("unexpected-equipment")]
        [InlineData("equipment-capacity")]
        [InlineData("equipment-area")]
        [InlineData("budget")]
        public async Task Submit_WhenSelectionViolatesRequirements_ThrowsConflictException(
            string scenario)
        {
            var organizerUserId = await CreateOrganizerUserAsync();
            var requiresEquipment = scenario is "equipment-capacity" or "equipment-area";
            var eventItem = await CreateEventAsync(
                organizerUserId,
                budget: scenario == "budget" ? 150m : 1000m,
                requiredSpeakerCount: scenario == "speaker-count" ? 2 : 1,
                requiresEquipment: requiresEquipment);
            var venue = await CreateResourceAsync(
                "Main Hall",
                ResourceType.Venue,
                scenario == "venue-capacity" ? 40 : 120);
            var speaker = await CreateResourceAsync(
                "Architecture Speaker",
                ResourceType.Speaker,
                area: scenario == "speaker-area" ? "Finance" : "IT");
            var equipmentPackage = await CreateResourceAsync(
                "Conference Equipment",
                ResourceType.EquipmentPackage,
                scenario == "equipment-capacity" ? 40 : 120,
                scenario == "equipment-area" ? "Finance" : "IT");

            if (scenario == "unavailable")
            {
                venue.MarkUnavailable(DateTime.UtcNow);
                await DbContext.SaveChangesAsync();
            }

            var booking = await CreateBookingAsync(eventItem);
            var selectedEquipmentId = requiresEquipment || scenario == "unexpected-equipment"
                ? equipmentPackage.Id
                : (Guid?)null;
            await UpdateDraftAsync(
                organizerUserId,
                eventItem.Id,
                booking.Version,
                scenario == "missing-venue" ? null : venue.Id,
                [speaker.Id],
                selectedEquipmentId);
            booking = await ReloadBookingAsync(eventItem.Id);

            await Assert.ThrowsAsync<ConflictException>(() =>
                CreateSubmitHandler(organizerUserId).Handle(
                    new SubmitEventBookingCommand(eventItem.Id, booking.Version),
                    CancellationToken.None));
        }

        [Fact]
        public async Task Submit_WhenEventAlreadyStarted_ThrowsConflictException()
        {
            var organizerUserId = await CreateOrganizerUserAsync();
            var startsAtUtc = DateTime.UtcNow.AddHours(-2);
            var eventItem = await CreateEventAsync(
                organizerUserId,
                startsAtUtc: startsAtUtc,
                endsAtUtc: startsAtUtc.AddHours(1));
            var (venue, speaker, _) = await CreateValidResourcesAsync();
            var booking = await CreateBookingAsync(eventItem);
            await UpdateDraftAsync(
                organizerUserId,
                eventItem.Id,
                booking.Version,
                venue.Id,
                [speaker.Id],
                null);
            booking = await ReloadBookingAsync(eventItem.Id);

            await Assert.ThrowsAsync<ConflictException>(() =>
                CreateSubmitHandler(organizerUserId).Handle(
                    new SubmitEventBookingCommand(eventItem.Id, booking.Version),
                    CancellationToken.None));
        }

        [Fact]
        public async Task UpdateDraft_WhenResourceHasWrongType_ThrowsConflictException()
        {
            var organizerUserId = await CreateOrganizerUserAsync();
            var eventItem = await CreateEventAsync(organizerUserId);
            var booking = await CreateBookingAsync(eventItem);
            var speaker = await CreateResourceAsync(
                "Architecture Speaker",
                ResourceType.Speaker,
                area: "IT");

            await Assert.ThrowsAsync<ConflictException>(() =>
                CreateUpdateHandler(organizerUserId).Handle(
                    new UpdateEventBookingDraftCommand(
                        eventItem.Id,
                        booking.Version,
                        speaker.Id,
                        [],
                        null),
                    CancellationToken.None));
        }

        [Fact]
        public async Task Withdraw_SubmittedBooking_ReturnsSameAggregateToDraft()
        {
            var organizerUserId = await CreateOrganizerUserAsync();
            var eventItem = await CreateEventAsync(organizerUserId);
            var booking = await CreateBookingAsync(eventItem);
            await SetBookingStatusAsync(
                booking.Id,
                EventResourceBookingStatus.Submitted,
                DateTime.UtcNow.AddHours(1));
            booking = await ReloadBookingAsync(eventItem.Id);
            var handler = CreateWithdrawHandler(organizerUserId);

            var response = await handler.Handle(
                new WithdrawEventBookingCommand(eventItem.Id, booking.Version),
                CancellationToken.None);

            Assert.Equal(booking.Id, response.Id);
            Assert.Equal(EventResourceBookingStatus.Draft.ToString(), response.Status);
            Assert.Null(response.HoldExpiresAtUtc);
        }

        [Fact]
        public async Task Revise_RejectedBooking_ReturnsSameAggregateToDraft()
        {
            var organizerUserId = await CreateOrganizerUserAsync();
            var eventItem = await CreateEventAsync(organizerUserId);
            var booking = await CreateBookingAsync(eventItem);
            await SetBookingStatusAsync(booking.Id, EventResourceBookingStatus.Rejected);
            booking = await ReloadBookingAsync(eventItem.Id);
            var handler = CreateReviseHandler(organizerUserId);

            var response = await handler.Handle(
                new ReviseEventBookingCommand(eventItem.Id, booking.Version),
                CancellationToken.None);

            Assert.Equal(booking.Id, response.Id);
            Assert.Equal(EventResourceBookingStatus.Draft.ToString(), response.Status);
        }

        [Fact]
        public async Task UpdateDraft_WithAdminUser_ThrowsForbiddenException()
        {
            var organizerUserId = await CreateOrganizerUserAsync();
            var eventItem = await CreateEventAsync(organizerUserId);
            var booking = await CreateBookingAsync(eventItem);
            var handler = CreateUpdateHandler(Guid.NewGuid(), ApplicationRoles.Admin);

            var action = () => handler.Handle(
                new UpdateEventBookingDraftCommand(eventItem.Id, booking.Version, null, [], null),
                CancellationToken.None);

            await Assert.ThrowsAsync<ForbiddenException>(action);
        }

        [Fact]
        public async Task UpdateDraft_WithStaleVersion_ThrowsConflictException()
        {
            var organizerUserId = await CreateOrganizerUserAsync();
            var eventItem = await CreateEventAsync(organizerUserId);
            await CreateBookingAsync(eventItem);
            var handler = CreateUpdateHandler(organizerUserId);

            var action = () => handler.Handle(
                new UpdateEventBookingDraftCommand(eventItem.Id, 999, null, [], null),
                CancellationToken.None);

            await Assert.ThrowsAsync<ConflictException>(action);
        }

        [Fact]
        public async Task Submit_WithStaleVersion_ThrowsConflictException()
        {
            var organizerUserId = await CreateOrganizerUserAsync();
            var eventItem = await CreateEventAsync(organizerUserId);
            await CreateBookingAsync(eventItem);

            await Assert.ThrowsAsync<ConflictException>(() =>
                CreateSubmitHandler(organizerUserId).Handle(
                    new SubmitEventBookingCommand(eventItem.Id, 999),
                    CancellationToken.None));
        }

        [Fact]
        public async Task Withdraw_WithStaleVersion_ThrowsConflictException()
        {
            var organizerUserId = await CreateOrganizerUserAsync();
            var eventItem = await CreateEventAsync(organizerUserId);
            var booking = await CreateBookingAsync(eventItem);
            await SetBookingStatusAsync(
                booking.Id,
                EventResourceBookingStatus.Submitted,
                DateTime.UtcNow.AddHours(1));

            await Assert.ThrowsAsync<ConflictException>(() =>
                CreateWithdrawHandler(organizerUserId).Handle(
                    new WithdrawEventBookingCommand(eventItem.Id, 999),
                    CancellationToken.None));
        }

        [Fact]
        public async Task Revise_WithStaleVersion_ThrowsConflictException()
        {
            var organizerUserId = await CreateOrganizerUserAsync();
            var eventItem = await CreateEventAsync(organizerUserId);
            var booking = await CreateBookingAsync(eventItem);
            await SetBookingStatusAsync(booking.Id, EventResourceBookingStatus.Rejected);

            await Assert.ThrowsAsync<ConflictException>(() =>
                CreateReviseHandler(organizerUserId).Handle(
                    new ReviseEventBookingCommand(eventItem.Id, 999),
                    CancellationToken.None));
        }

        private async Task UpdateDraftAsync(
            Guid organizerUserId,
            Guid eventId,
            int version,
            Guid? venueId,
            IReadOnlyList<Guid> speakerIds,
            Guid? equipmentPackageId)
        {
            await CreateUpdateHandler(organizerUserId).Handle(
                new UpdateEventBookingDraftCommand(
                    eventId,
                    version,
                    venueId,
                    speakerIds,
                    equipmentPackageId),
                CancellationToken.None);
        }

        private async Task<EventResourceBooking> ReloadBookingAsync(Guid eventId)
        {
            DbContext.ChangeTracker.Clear();

            return await DbContext.EventResourceBookings
                .Include(booking => booking.Items)
                .SingleAsync(booking => booking.EventId == eventId);
        }

        private async Task<(Resource Venue, Resource Speaker, Resource EquipmentPackage)> CreateValidResourcesAsync()
        {
            var venue = await CreateResourceAsync("Main Hall", ResourceType.Venue, capacity: 120);
            var speaker = await CreateResourceAsync("Architecture Speaker", ResourceType.Speaker, area: "IT");
            var equipmentPackage = await CreateResourceAsync(
                "Conference Equipment",
                ResourceType.EquipmentPackage,
                capacity: 120,
                area: "IT");

            return (venue, speaker, equipmentPackage);
        }

        private async Task<Resource> CreateResourceAsync(
            string name,
            ResourceType type,
            int? capacity = null,
            string? area = null,
            decimal cost = 100m)
        {
            var resource = TestResourceFactory.Create(
                name,
                $"Description for {name}.",
                type,
                cost,
                capacity,
                area,
                4,
                DateTime.UtcNow);

            DbContext.Resources.Add(resource);
            await DbContext.SaveChangesAsync();

            return resource;
        }

        private UpdateEventBookingDraftCommandHandler CreateUpdateHandler(
            Guid userId,
            params string[] roles)
        {
            DbContext.ChangeTracker.Clear();

            return new UpdateEventBookingDraftCommandHandler(
                DbContext,
                new EventAuthorizationService(new TestCurrentUserService(
                    userId,
                    roles.Length == 0 ? [ApplicationRoles.Organizer] : roles)));
        }

        private SubmitEventBookingCommandHandler CreateSubmitHandler(Guid userId)
        {
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

        private WithdrawEventBookingCommandHandler CreateWithdrawHandler(Guid userId)
        {
            return new WithdrawEventBookingCommandHandler(
                DbContext,
                new EventAuthorizationService(new TestCurrentUserService(
                    userId,
                    ApplicationRoles.Organizer)));
        }

        private ReviseEventBookingCommandHandler CreateReviseHandler(Guid userId)
        {
            return new ReviseEventBookingCommandHandler(
                DbContext,
                new EventAuthorizationService(new TestCurrentUserService(
                    userId,
                    ApplicationRoles.Organizer)));
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
