using EventOrganizer.Application.Queries.ListResources;
using EventOrganizer.Domain.Resources;

namespace EventOrganizer.Tests.Application.Resources
{
    public sealed class ListResourcesQueryHandlerTests : ApplicationTestBase
    {
        [Fact]
        public async Task Handle_ReturnsResourcesOrderedByName()
        {
            var projector = TestResourceFactory.Create(
                "Projector",
                "4K projector.",
                ResourceType.EquipmentPackage,
                100m,
                null,
                null,
                3,
                DateTime.UtcNow);

            var hall = TestResourceFactory.Create(
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

        [Fact]
        public async Task Handle_ReturnsTypeSpecificResourceFields()
        {
            var speaker = Speaker.Create(
                "Architecture Speaker",
                "Speaker profile.",
                200m,
                "IT",
                5,
                DateTime.UtcNow);
            var equipmentPackage = EquipmentPackage.Create(
                "Conference AV Package",
                "Audio and video package.",
                300m,
                "AV Supplier",
                150,
                "Belgrade",
                true,
                "Projector, microphones and setup.",
                4,
                DateTime.UtcNow);

            DbContext.Resources.AddRange(speaker, equipmentPackage);
            await DbContext.SaveChangesAsync();

            var handler = new ListResourcesQueryHandler(DbContext);

            var result = await handler.Handle(
                new ListResourcesQuery(),
                CancellationToken.None);

            var speakerResponse = Assert.Single(result, item => item.Id == speaker.Id);
            Assert.Equal("IT", speakerResponse.ExpertiseArea);
            Assert.Null(speakerResponse.ProviderName);

            var packageResponse = Assert.Single(
                result,
                item => item.Id == equipmentPackage.Id);
            Assert.Equal("AV Supplier", packageResponse.ProviderName);
            Assert.Equal(150, packageResponse.SupportedCapacity);
            Assert.Equal("Belgrade", packageResponse.ServiceArea);
            Assert.True(packageResponse.IncludesTechnicalSupport);
            Assert.Equal(
                "Projector, microphones and setup.",
                packageResponse.ContentsSummary);
            Assert.Null(packageResponse.Capacity);
            Assert.Null(packageResponse.ExpertiseArea);
        }
    }
}
