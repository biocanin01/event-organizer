using EventOrganizer.Application.Queries.ListResources;
using EventOrganizer.Domain.Resources;

namespace EventOrganizer.Tests.Application.Resources
{
    public sealed class ListResourcesQueryHandlerTests : ApplicationTestBase
    {
        [Fact]
        public async Task Handle_ReturnsResourcesOrderedByName()
        {
            var projector = Resource.Create(
                "Projector",
                "4K projector.",
                ResourceType.Equipment,
                DateTime.UtcNow);

            var hall = Resource.Create(
                "Conference Hall",
                "Main conference hall.",
                ResourceType.Venue,
                DateTime.UtcNow);

            DbContext.Resources.AddRange(projector, hall);
            await DbContext.SaveChangesAsync();

            var handler = new ListResourcesQueryHandler(DbContext);

            var result = await handler.Handle(
                new ListResourcesQuery(),
                CancellationToken.None);

            Assert.Equal(2, result.Count);
            Assert.Equal(hall.Id, result[0].Id);
            Assert.Equal(projector.Id, result[1].Id);
        }
    }
}