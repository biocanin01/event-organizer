using EventOrganizer.Application.Commands.CancelEvent;
using EventOrganizer.Application.Common.Exceptions;
using EventOrganizer.Domain.Events;

namespace EventOrganizer.Tests.Application.Events
{
    public sealed class CancelEventCommandHandlerTests : ApplicationTestBase
    {
        [Fact]
        public async Task Handle_WhenEventExists_CancelsEvent()
        {
            var eventItem = await CreateEventAsync();
            var handler = new CancelEventCommandHandler(DbContext);

            await handler.Handle(
                new CancelEventCommand(eventItem.Id),
                CancellationToken.None);

            Assert.Equal(EventStatus.Cancelled, eventItem.Status);
            Assert.NotNull(eventItem.UpdatedAtUtc);
        }

        [Fact]
        public async Task Handle_WhenEventDoesNotExist_ThrowsNotFoundException()
        {
            var handler = new CancelEventCommandHandler(DbContext);

            var act = () => handler.Handle(
                new CancelEventCommand(Guid.NewGuid()),
                CancellationToken.None);

            await Assert.ThrowsAsync<NotFoundException>(act);
        }
    }
}