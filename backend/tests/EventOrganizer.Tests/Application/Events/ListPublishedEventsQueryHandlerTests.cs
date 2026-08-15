using EventOrganizer.Application.Queries.ListPublishedEvents;
using EventOrganizer.Domain.Registrations;

namespace EventOrganizer.Tests.Application.Events
{
    public sealed class ListPublishedEventsQueryHandlerTests : ApplicationTestBase
    {
        [Fact]
        public async Task Handle_ReturnsOnlyPublishedEventsOrderedByStartDate()
        {
            var now = DateTime.UtcNow;
            var draftEvent = await CreateEventAsync(
                title: "Draft event",
                startsAtUtc: now.AddDays(3));

            var laterPublishedEvent = await CreateEventAsync(
                title: "Later published event",
                startsAtUtc: now.AddDays(10));

            var earlierPublishedEvent = await CreateEventAsync(
                title: "Earlier published event",
                startsAtUtc: now.AddDays(2));

            var cancelledEvent = await CreateEventAsync(
                title: "Cancelled event",
                startsAtUtc: now.AddDays(4));

            var pastPublishedEvent = await CreateEventAsync(
                title: "Past published event",
                startsAtUtc: now.AddDays(-2),
                endsAtUtc: now.AddDays(-2).AddHours(4));

            laterPublishedEvent.Publish(new DateTime(2026, 8, 1, 13, 0, 0, DateTimeKind.Utc));
            earlierPublishedEvent.Publish(new DateTime(2026, 8, 1, 13, 0, 0, DateTimeKind.Utc));
            cancelledEvent.Publish(new DateTime(2026, 8, 1, 13, 0, 0, DateTimeKind.Utc));
            pastPublishedEvent.Publish(now.AddDays(-3));
            cancelledEvent.Cancel(new DateTime(2026, 8, 1, 14, 0, 0, DateTimeKind.Utc));
            await DbContext.SaveChangesAsync();

            var handler = new ListPublishedEventsQueryHandler(DbContext);

            var result = await handler.Handle(
                new ListPublishedEventsQuery(),
                CancellationToken.None);

            Assert.Equal(2, result.Count);
            Assert.Equal(earlierPublishedEvent.Id, result[0].Id);
            Assert.Equal(laterPublishedEvent.Id, result[1].Id);
            Assert.DoesNotContain(result, eventItem => eventItem.Id == draftEvent.Id);
            Assert.DoesNotContain(result, eventItem => eventItem.Id == cancelledEvent.Id);
            Assert.DoesNotContain(result, eventItem => eventItem.Id == pastPublishedEvent.Id);
        }

        [Fact]
        public async Task Handle_CountsOnlyConfirmedRegistrations()
        {
            var organizerUserId = await CreateOrganizerUserAsync();
            var decisionUserId = organizerUserId;
            var eventItem = await CreateEventAsync(
                organizerUserId,
                startsAtUtc: DateTime.UtcNow.AddDays(5));
            eventItem.Publish(DateTime.UtcNow);
            var pendingUserId = await CreateOrganizerUserAsync("pending-count@example.com");
            var confirmedUserId = await CreateOrganizerUserAsync("confirmed-count@example.com");
            var rejectedUserId = await CreateOrganizerUserAsync("rejected-count@example.com");
            var cancelledUserId = await CreateOrganizerUserAsync("cancelled-count@example.com");
            var pending = Registration.Create(eventItem.Id, pendingUserId, DateTime.UtcNow);
            var confirmed = Registration.Create(eventItem.Id, confirmedUserId, DateTime.UtcNow);
            confirmed.Confirm(decisionUserId, DateTime.UtcNow);
            var rejected = Registration.Create(eventItem.Id, rejectedUserId, DateTime.UtcNow);
            rejected.Reject("No capacity.", decisionUserId, DateTime.UtcNow);
            var cancelled = Registration.Create(eventItem.Id, cancelledUserId, DateTime.UtcNow);
            cancelled.Cancel(DateTime.UtcNow);
            DbContext.Registrations.AddRange(pending, confirmed, rejected, cancelled);
            await DbContext.SaveChangesAsync();

            var result = await new ListPublishedEventsQueryHandler(DbContext).Handle(
                new ListPublishedEventsQuery(),
                CancellationToken.None);

            var response = Assert.Single(result);
            Assert.Equal(1, response.ConfirmedRegistrationCount);
        }
    }
}
