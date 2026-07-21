using EventOrganizer.Application.Commands.CreateResource;
using EventOrganizer.Domain.Resources;
using Microsoft.EntityFrameworkCore;

namespace EventOrganizer.Tests.Application.Resources
{
    public sealed class CreateResourceCommandHandlerTests : ApplicationTestBase
    {
        [Fact]
        public async Task Handle_CreatesAvailableResourceAndReturnsId()
        {
            var handler = new CreateResourceCommandHandler(DbContext);
            var command = new CreateResourceCommand(
                "Main Conference Hall",
                "A hall suitable for conferences with up to 200 participants.",
                ResourceType.Venue);

            var resourceId = await handler.Handle(command, CancellationToken.None);

            var resource = await DbContext.Resources
                .FirstOrDefaultAsync(resource => resource.Id == resourceId);

            Assert.NotNull(resource);
            Assert.Equal(command.Name, resource.Name);
            Assert.Equal(command.Description, resource.Description);
            Assert.Equal(command.Type, resource.Type);
            Assert.Equal(ResourceStatus.Available, resource.Status);
            Assert.NotEqual(Guid.Empty, resourceId);
            Assert.NotEqual(default, resource.CreatedAtUtc);
        }
    }
}
