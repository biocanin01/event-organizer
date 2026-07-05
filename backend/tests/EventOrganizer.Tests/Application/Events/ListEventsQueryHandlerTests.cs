using EventOrganizer.Application.Queries.ListEvents;

namespace EventOrganizer.Tests.Application.Events
{
    public sealed class ListEventsQueryHandlerTests : ApplicationTestBase
    {
        [Fact]
        public async Task Handle_ReturnsEventsOrderedByStartDate()
        {
            var laterStart = new DateTime(2026, 10, 1, 9, 0, 0, DateTimeKind.Utc);
            var earlierStart = new DateTime(2026, 9, 1, 9, 0, 0, DateTimeKind.Utc);

            var laterEvent = await CreateEventAsync(
                title: "Later event",
                startsAtUtc: laterStart);

            var earlierEvent = await CreateEventAsync(
                title: "Earlier event",
                startsAtUtc: earlierStart);

            var handler = new ListEventsQueryHandler(DbContext);

            var result = await handler.Handle(
                new ListEventsQuery(),
                CancellationToken.None);

            Assert.Equal(2, result.Count);
            Assert.Equal(earlierEvent.Id, result[0].Id);
            Assert.Equal(laterEvent.Id, result[1].Id);
        }
    }
}