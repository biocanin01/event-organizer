using EventOrganizer.Application.Commands.CreateResource;
using EventOrganizer.Domain.Resources;
using Microsoft.EntityFrameworkCore;

namespace EventOrganizer.Tests.Application.Resources
{
    public sealed class CreateResourceCommandHandlerTests : ApplicationTestBase
    {
        [Fact]
        public async Task Handle_WithVenueCommand_CreatesAvailableVenueAndReturnsId()
        {
            var handler = new CreateResourceCommandHandler(DbContext);
            var command = new CreateResourceCommand(
                "Main Conference Hall",
                "A hall suitable for conferences with up to 200 participants.",
                ResourceType.Venue,
                500m,
                4,
                200,
                null,
                null,
                null,
                null,
                null,
                null);

            var resourceId = await handler.Handle(command, CancellationToken.None);

            var resource = await DbContext.Resources
                .OfType<Venue>()
                .FirstOrDefaultAsync(resource => resource.Id == resourceId);

            Assert.NotNull(resource);
            Assert.Equal(command.Name, resource.Name);
            Assert.Equal(command.Description, resource.Description);
            Assert.Equal(command.Type, resource.Type);
            Assert.Equal(command.Cost, resource.Cost);
            Assert.Equal(command.Capacity, resource.Capacity);
            Assert.Equal(command.QualityScore, resource.QualityScore);
            Assert.Equal(ResourceStatus.Available, resource.Status);
            Assert.Equal(1, resource.Version);
            Assert.NotEqual(Guid.Empty, resourceId);
            Assert.NotEqual(default, resource.CreatedAtUtc);
        }

        [Fact]
        public async Task Handle_WithEquipmentPackageCommand_CreatesAvailablePackage()
        {
            var handler = new CreateResourceCommandHandler(DbContext);
            var command = new CreateResourceCommand(
                "Conference AV package",
                "Audio and video package.",
                ResourceType.EquipmentPackage,
                250m,
                5,
                null,
                null,
                "AV Supplier",
                150,
                "Belgrade",
                true,
                "Projector, microphones and on-site setup.");

            var resourceId = await handler.Handle(command, CancellationToken.None);

            var resource = await DbContext.Resources
                .OfType<EquipmentPackage>()
                .FirstOrDefaultAsync(resource => resource.Id == resourceId);

            Assert.NotNull(resource);
            Assert.Equal(command.ProviderName, resource.ProviderName);
            Assert.Equal(command.SupportedCapacity, resource.SupportedCapacity);
            Assert.Equal(command.ServiceArea, resource.ServiceArea);
            Assert.Equal(command.IncludesTechnicalSupport, resource.IncludesTechnicalSupport);
            Assert.Equal(command.ContentsSummary, resource.ContentsSummary);
        }
    }
}
