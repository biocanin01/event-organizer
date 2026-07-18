using EventOrganizer.Application.Common.Exceptions;
using EventOrganizer.Application.Queries.GetPublishedEventById;

namespace EventOrganizer.Tests.Application.Events
{
    public sealed class GetPublishedEventByIdQueryHandlerTests : ApplicationTestBase
    {
        [Fact]
        public async Task Handle_WhenPublishedEventExists_ReturnsEvent()
        {
            var eventItem = await CreateEventAsync();
            eventItem.Publish(new DateTime(2026, 8, 1, 13, 0, 0, DateTimeKind.Utc));
            await DbContext.SaveChangesAsync();

            var handler = new GetPublishedEventByIdQueryHandler(DbContext);

            var result = await handler.Handle(
                new GetPublishedEventByIdQuery(eventItem.Id),
                CancellationToken.None);

            Assert.Equal(eventItem.Id, result.Id);
            Assert.Equal(eventItem.Title, result.Title);
            Assert.Equal(eventItem.Status.ToString(), result.Status);
        }

        [Fact]
        public async Task Handle_WhenEventDoesNotExist_ThrowsNotFoundException()
        {
            var handler = new GetPublishedEventByIdQueryHandler(DbContext);

            await Assert.ThrowsAsync<NotFoundException>(() =>
                handler.Handle(
                    new GetPublishedEventByIdQuery(Guid.NewGuid()),
                    CancellationToken.None));
        }

        [Fact]
        public async Task Handle_WhenEventIsDraft_ThrowsNotFoundException()
        {
            var eventItem = await CreateEventAsync();
            var handler = new GetPublishedEventByIdQueryHandler(DbContext);

            await Assert.ThrowsAsync<NotFoundException>(() =>
                handler.Handle(
                    new GetPublishedEventByIdQuery(eventItem.Id),
                    CancellationToken.None));
        }

        [Fact]
        public async Task Handle_WhenEventIsCancelled_ThrowsNotFoundException()
        {
            var eventItem = await CreateEventAsync();
            eventItem.Publish(new DateTime(2026, 8, 1, 13, 0, 0, DateTimeKind.Utc));
            eventItem.Cancel(new DateTime(2026, 8, 1, 14, 0, 0, DateTimeKind.Utc));
            await DbContext.SaveChangesAsync();

            var handler = new GetPublishedEventByIdQueryHandler(DbContext);

            await Assert.ThrowsAsync<NotFoundException>(() =>
                handler.Handle(
                    new GetPublishedEventByIdQuery(eventItem.Id),
                    CancellationToken.None));
        }
    }
}
