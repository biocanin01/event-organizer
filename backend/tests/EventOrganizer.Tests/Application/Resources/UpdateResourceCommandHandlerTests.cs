using EventOrganizer.Application.Commands.UpdateResource;
using EventOrganizer.Application.Common.Exceptions;
using EventOrganizer.Domain.Resources;
using Microsoft.EntityFrameworkCore;

namespace EventOrganizer.Tests.Application.Resources
{
    public sealed class UpdateResourceCommandHandlerTests : ApplicationTestBase
    {
        [Fact]
        public async Task Handle_WhenVenueExists_UpdatesDetails()
        {
            var resource = await CreateVenueAsync();
            var handler = new UpdateResourceCommandHandler(DbContext);
            var command = new UpdateResourceCommand(
                resource.Id,
                "Main Conference Hall",
                "A hall suitable for conferences with up to 200 participants.",
                ResourceType.Venue,
                650m,
                5,
                200,
                null,
                null,
                null,
                null,
                null,
                null);

            await handler.Handle(command, CancellationToken.None);

            DbContext.ChangeTracker.Clear();

            var updatedResource = await DbContext.Resources
                .OfType<Venue>()
                .SingleAsync(resource => resource.Id == command.ResourceId);

            Assert.Equal(command.Name, updatedResource.Name);
            Assert.Equal(command.Description, updatedResource.Description);
            Assert.Equal(command.Cost, updatedResource.Cost);
            Assert.Equal(command.Capacity, updatedResource.Capacity);
            Assert.Equal(command.QualityScore, updatedResource.QualityScore);
            Assert.Equal(2, updatedResource.Version);
            Assert.NotNull(updatedResource.UpdatedAtUtc);
        }

        [Fact]
        public async Task Handle_WhenPackageExists_UpdatesPackageDetails()
        {
            var resource = EquipmentPackage.Create(
                "Conference AV package",
                "Audio and video package.",
                250m,
                "AV Supplier",
                150,
                "Belgrade",
                true,
                "Projector and microphones.",
                4,
                DateTime.UtcNow);

            DbContext.Resources.Add(resource);
            await DbContext.SaveChangesAsync();

            var handler = new UpdateResourceCommandHandler(DbContext);
            var command = new UpdateResourceCommand(
                resource.Id,
                "Premium AV package",
                "Expanded audio and video package.",
                ResourceType.EquipmentPackage,
                350m,
                5,
                null,
                null,
                "Premium Supplier",
                250,
                "Novi Sad",
                false,
                "Projector, microphones, lights and setup.");

            await handler.Handle(command, CancellationToken.None);

            DbContext.ChangeTracker.Clear();

            var updatedResource = await DbContext.Resources
                .OfType<EquipmentPackage>()
                .SingleAsync(resource => resource.Id == command.ResourceId);

            Assert.Equal(command.ProviderName, updatedResource.ProviderName);
            Assert.Equal(command.SupportedCapacity, updatedResource.SupportedCapacity);
            Assert.Equal(command.ServiceArea, updatedResource.ServiceArea);
            Assert.Equal(command.IncludesTechnicalSupport, updatedResource.IncludesTechnicalSupport);
            Assert.Equal(command.ContentsSummary, updatedResource.ContentsSummary);
        }

        [Fact]
        public async Task Handle_WhenResourceTypeChanges_ThrowsConflictException()
        {
            var resource = await CreateVenueAsync();
            var handler = new UpdateResourceCommandHandler(DbContext);

            var action = () => handler.Handle(
                new UpdateResourceCommand(
                    resource.Id,
                    "Main Conference Hall",
                    "A hall suitable for conferences.",
                    ResourceType.Speaker,
                    650m,
                    5,
                    null,
                    "IT",
                    null,
                    null,
                    null,
                    null,
                    null),
                CancellationToken.None);

            await Assert.ThrowsAsync<ConflictException>(action);
        }

        [Fact]
        public async Task Handle_WhenResourceDoesNotExist_ThrowsNotFoundException()
        {
            var handler = new UpdateResourceCommandHandler(DbContext);

            var action = () => handler.Handle(
                new UpdateResourceCommand(
                    Guid.NewGuid(),
                    "Main Conference Hall",
                    "A hall suitable for conferences with up to 200 participants.",
                    ResourceType.Venue,
                    650m,
                    5,
                    200,
                    null,
                    null,
                    null,
                    null,
                    null,
                    null),
                CancellationToken.None);

            await Assert.ThrowsAsync<NotFoundException>(action);
        }

        [Fact]
        public async Task Handle_WhenResourceIsArchived_ThrowsInvalidOperationException()
        {
            var resource = await CreateVenueAsync();
            resource.Archive(DateTime.UtcNow);
            await DbContext.SaveChangesAsync();

            var handler = new UpdateResourceCommandHandler(DbContext);

            var action = () => handler.Handle(
                new UpdateResourceCommand(
                    resource.Id,
                    "Main Conference Hall",
                    "A hall suitable for conferences with up to 200 participants.",
                    ResourceType.Venue,
                    650m,
                    5,
                    200,
                    null,
                    null,
                    null,
                    null,
                    null,
                    null),
                CancellationToken.None);

            await Assert.ThrowsAsync<InvalidOperationException>(action);
        }

        private async Task<Venue> CreateVenueAsync()
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

            return resource;
        }
    }
}
