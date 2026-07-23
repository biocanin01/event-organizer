using EventOrganizer.Application.Commands.MarkResourceAvailable;
using EventOrganizer.Domain.Resources;
using Microsoft.EntityFrameworkCore;

namespace EventOrganizer.Tests.Application.Resources
{
    public sealed class MarkResourceAvailableCommandHandlerTests : ApplicationTestBase
    {
        [Fact]
        public async Task Handle_WhenResourceExists_MarksResourceAsAvailable()
        {
            var resource = await CreateUnavailableResourceAsync();
            var handler = new MarkResourceAvailableCommandHandler(DbContext);
            var command = new MarkResourceAvailableCommand(resource.Id);

            await handler.Handle(command, CancellationToken.None);

            DbContext.ChangeTracker.Clear();

            var updatedResource = await DbContext.Resources
                .SingleAsync(resource => resource.Id == command.ResourceId);

            Assert.Equal(ResourceStatus.Available, updatedResource.Status);
            Assert.NotNull(updatedResource.UpdatedAtUtc);
        }

        private async Task<Resource> CreateUnavailableResourceAsync()
        {
            var resource = Resource.Create(
                "Projector",
                "Conference room projector.",
                ResourceType.Equipment,
                DateTime.UtcNow);

            resource.MarkUnavailable(DateTime.UtcNow);

            DbContext.Resources.Add(resource);
            await DbContext.SaveChangesAsync();

            return resource;
        }
    }
}
