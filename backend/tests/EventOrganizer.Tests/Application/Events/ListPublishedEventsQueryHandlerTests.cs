using EventOrganizer.Application.Queries.ListPublishedEvents;

namespace EventOrganizer.Tests.Application.Events
{
    public sealed class ListPublishedEventsQueryHandlerTests : ApplicationTestBase
    {
        [Fact]
        public async Task Handle_ReturnsOnlyPublishedEventsOrderedByStartDate()
        {
            var draftEvent = await CreateEventAsync(
                title: "Draft event",
                startsAtUtc: new DateTime(2026, 9, 2, 9, 0, 0, DateTimeKind.Utc));

            var laterPublishedEvent = await CreateEventAsync(
                title: "Later published event",
                startsAtUtc: new DateTime(2026, 10, 1, 9, 0, 0, DateTimeKind.Utc));

            var earlierPublishedEvent = await CreateEventAsync(
                title: "Earlier published event",
                startsAtUtc: new DateTime(2026, 9, 1, 9, 0, 0, DateTimeKind.Utc));

            var cancelledEvent = await CreateEventAsync(
                title: "Cancelled event",
                startsAtUtc: new DateTime(2026, 9, 3, 9, 0, 0, DateTimeKind.Utc));

            laterPublishedEvent.Publish(new DateTime(2026, 8, 1, 13, 0, 0, DateTimeKind.Utc));
            earlierPublishedEvent.Publish(new DateTime(2026, 8, 1, 13, 0, 0, DateTimeKind.Utc));
            cancelledEvent.Publish(new DateTime(2026, 8, 1, 13, 0, 0, DateTimeKind.Utc));
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
        }
    }
}
