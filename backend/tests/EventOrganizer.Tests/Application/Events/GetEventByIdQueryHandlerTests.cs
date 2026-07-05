using EventOrganizer.Application.Queries.GetEventById;

namespace EventOrganizer.Tests.Application.Events
{
    public sealed class GetEventByIdQueryHandlerTests : ApplicationTestBase
    {
        [Fact]
        public async Task Handle_WhenEventExists_ReturnsEvent()
        {
            var eventItem = await CreateEventAsync();
            var handler = new GetEventByIdQueryHandler(DbContext);

            var result = await handler.Handle(
                new GetEventByIdQuery(eventItem.Id),
                CancellationToken.None);

            Assert.NotNull(result);
            Assert.Equal(eventItem.Id, result.Id);
            Assert.Equal(eventItem.Title, result.Title);
            Assert.Equal(eventItem.Status.ToString(), result.Status);
        }

        [Fact]
        public async Task Handle_WhenEventDoesNotExist_ReturnsNull()
        {
            var handler = new GetEventByIdQueryHandler(DbContext);

            var result = await handler.Handle(
                new GetEventByIdQuery(Guid.NewGuid()),
                CancellationToken.None);

            Assert.Null(result);
        }
    }
}