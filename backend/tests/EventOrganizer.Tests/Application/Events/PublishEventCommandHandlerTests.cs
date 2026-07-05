using EventOrganizer.Application.Commands.PublishEvent;
using EventOrganizer.Application.Common.Exceptions;
using EventOrganizer.Domain.Events;

namespace EventOrganizer.Tests.Application.Events
{
    public sealed class PublishEventCommandHandlerTests : ApplicationTestBase
    {
        [Fact]
        public async Task Handle_WhenEventExists_PublishesEvent()
        {
            var eventItem = await CreateEventAsync();
            var handler = new PublishEventCommandHandler(DbContext);

            await handler.Handle(
                new PublishEventCommand(eventItem.Id),
                CancellationToken.None);

            Assert.Equal(EventStatus.Published, eventItem.Status);
            Assert.NotNull(eventItem.UpdatedAtUtc);
        }

        [Fact]
        public async Task Handle_WhenEventDoesNotExist_ThrowsNotFoundException()
        {
            var handler = new PublishEventCommandHandler(DbContext);

            var act = () => handler.Handle(
                new PublishEventCommand(Guid.NewGuid()),
                CancellationToken.None);

            await Assert.ThrowsAsync<NotFoundException>(act);
        }
    }
}