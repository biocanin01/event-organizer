using EventOrganizer.Application.Commands.MarkResourceUnavailable;
using EventOrganizer.Application.Common.Exceptions;
using EventOrganizer.Domain.Resources;
using Microsoft.EntityFrameworkCore;

namespace EventOrganizer.Tests.Application.Resources
{
    public sealed class MarkResourceUnavailableCommandHandlerTests : ApplicationTestBase
    {
        [Fact]
        public async Task Handle_WhenResourceExists_MarksResourceAsUnavailable()
        {
            var resource = await CreateResourceAsync();
            var handler = new MarkResourceUnavailableCommandHandler(DbContext);
            var command = new MarkResourceUnavailableCommand(resource.Id);

            await handler.Handle(command, CancellationToken.None);

            DbContext.ChangeTracker.Clear();

            var updatedResource = await DbContext.Resources
                .SingleAsync(resource => resource.Id == command.ResourceId);

            Assert.Equal(ResourceStatus.Unavailable, updatedResource.Status);
            Assert.NotNull(updatedResource.UpdatedAtUtc);
        }

        [Fact]
        public async Task Handle_WhenResourceDoesNotExist_ThrowsNotFoundException()
        {
            var handler = new MarkResourceUnavailableCommandHandler(DbContext);

            var action = () => handler.Handle(
                new MarkResourceUnavailableCommand(Guid.NewGuid()),
                CancellationToken.None);

            await Assert.ThrowsAsync<NotFoundException>(action);
        }

        [Fact]
        public async Task Handle_WhenResourceIsArchived_ThrowsInvalidOperationException()
        {
            var resource = await CreateArchivedResourceAsync();
            var handler = new MarkResourceUnavailableCommandHandler(DbContext);

            var action = () => handler.Handle(
                new MarkResourceUnavailableCommand(resource.Id),
                CancellationToken.None);

            await Assert.ThrowsAsync<InvalidOperationException>(action);
        }

        private async Task<Resource> CreateResourceAsync()
        {
            var resource = Resource.Create(
                "Projector",
                "Conference room projector.",
                ResourceType.Equipment,
                DateTime.UtcNow);

            DbContext.Resources.Add(resource);
            await DbContext.SaveChangesAsync();

            return resource;
        }

        private async Task<Resource> CreateArchivedResourceAsync()
        {
            var resource = Resource.Create(
                "Projector",
                "Conference room projector.",
                ResourceType.Equipment,
                DateTime.UtcNow);

            resource.Archive(DateTime.UtcNow);

            DbContext.Resources.Add(resource);
            await DbContext.SaveChangesAsync();

            return resource;
        }
    }
}
