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
                100m,
                null,
                null,
                3,
                DateTime.UtcNow);

            var hall = Resource.Create(
                "Conference Hall",
                "Main conference hall.",
                ResourceType.Venue,
                500m,
                150,
                "IT",
                4,
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
            Assert.Equal(hall.Cost, result[0].Cost);
            Assert.Equal(projector.QualityScore, result[1].QualityScore);
        }
    }
}
