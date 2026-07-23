using EventOrganizer.Application.Commands.UpdateResource;
using EventOrganizer.Application.Common.Exceptions;
using EventOrganizer.Domain.Resources;
using Microsoft.EntityFrameworkCore;

namespace EventOrganizer.Tests.Application.Resources
{
    public sealed class UpdateResourceCommandHandlerTests : ApplicationTestBase
    {
        [Fact]
        public async Task Handle_WhenResourceExists_UpdatesDetails()
        {
            var resource = await CreateResourceAsync();
            var handler = new UpdateResourceCommandHandler(DbContext);
            var command = new UpdateResourceCommand(
                resource.Id,
                "Main Conference Hall",
                "A hall suitable for conferences with up to 200 participants.");

            await handler.Handle(command, CancellationToken.None);

            DbContext.ChangeTracker.Clear();

            var updatedResource = await DbContext.Resources
                .SingleAsync(resource => resource.Id == command.ResourceId);

            Assert.Equal(command.Name, updatedResource.Name);
            Assert.Equal(command.Description, updatedResource.Description);
            Assert.NotNull(updatedResource.UpdatedAtUtc);
        }

        [Fact]
        public async Task Handle_WhenResourceDoesNotExist_ThrowsNotFoundException()
        {
            var handler = new UpdateResourceCommandHandler(DbContext);

            var action = () => handler.Handle(
                new UpdateResourceCommand(
                    Guid.NewGuid(),
                    "Main Conference Hall",
                    "A hall suitable for conferences with up to 200 participants."),
                CancellationToken.None);

            await Assert.ThrowsAsync<NotFoundException>(action);
        }

        [Fact]
        public async Task Handle_WhenResourceIsArchived_ThrowsInvalidOperationException()
        {
            var resource = await CreateResourceAsync();
            resource.Archive(DateTime.UtcNow);
            await DbContext.SaveChangesAsync();

            var handler = new UpdateResourceCommandHandler(DbContext);

            var action = () => handler.Handle(
                new UpdateResourceCommand(
                    resource.Id,
                    "Main Conference Hall",
                    "A hall suitable for conferences with up to 200 participants."),
                CancellationToken.None);

            await Assert.ThrowsAsync<InvalidOperationException>(action);
        }

        private async Task<Resource> CreateResourceAsync()
        {
            var resource = Resource.Create(
                "Conference Hall",
                "Main conference hall.",
                ResourceType.Venue,
                DateTime.UtcNow);

            DbContext.Resources.Add(resource);
            await DbContext.SaveChangesAsync();

            return resource;
        }
    }
}
