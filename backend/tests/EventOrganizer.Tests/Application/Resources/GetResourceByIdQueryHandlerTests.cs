using EventOrganizer.Application.Queries.GetResourceById;
using EventOrganizer.Domain.Resources;

namespace EventOrganizer.Tests.Application.Resources
{
    public sealed class GetResourceByIdQueryHandlerTests : ApplicationTestBase
    {
        [Fact]
        public async Task Handle_WhenResourceExists_ReturnsResource()
        {
            var resource = Venue.Create(
                "Conference Hall",
                "Main conference hall.",
                500m,
                150,
                4,
                DateTime.UtcNow);

            DbContext.Resources.Add(resource);
            await DbContext.SaveChangesAsync();

            var handler = new GetResourceByIdQueryHandler(DbContext);

            var result = await handler.Handle(
                new GetResourceByIdQuery(resource.Id),
                CancellationToken.None);

            Assert.NotNull(result);
            Assert.Equal(resource.Id, result.Id);
            Assert.Equal(resource.Name, result.Name);
            Assert.Equal(resource.Type.ToString(), result.Type);
            Assert.Equal(resource.Status.ToString(), result.Status);
            Assert.Equal(resource.Cost, result.Cost);
            Assert.Equal(resource.Capacity, result.Capacity);
            Assert.Null(result.ExpertiseArea);
            Assert.Equal(resource.QualityScore, result.QualityScore);
            Assert.Equal(resource.Version, result.Version);
        }

        [Fact]
        public async Task Handle_WhenResourceDoesNotExist_ReturnsNull()
        {
            var handler = new GetResourceByIdQueryHandler(DbContext);

            var result = await handler.Handle(
                new GetResourceByIdQuery(Guid.NewGuid()),
                CancellationToken.None);

            Assert.Null(result);
        }
    }
}
