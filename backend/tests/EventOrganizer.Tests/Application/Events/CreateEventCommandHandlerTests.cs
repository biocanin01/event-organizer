using EventOrganizer.Application.Commands.CreateEvent;
using EventOrganizer.Application.Common.Exceptions;
using EventOrganizer.Application.Common.Interfaces;
using EventOrganizer.Domain.Events;
using Microsoft.EntityFrameworkCore;

namespace EventOrganizer.Tests.Application.Events
{
    public sealed class CreateEventCommandHandlerTests : ApplicationTestBase
    {
        [Fact]
        public async Task Handle_WithAuthenticatedUser_CreatesEventAndReturnsId()
        {
            var organizerUserId = await CreateOrganizerUserAsync();
            var handler = new CreateEventCommandHandler(
                DbContext,
                new TestCurrentUserService(organizerUserId));

            var startsAtUtc = new DateTime(2026, 9, 1, 9, 0, 0, DateTimeKind.Utc);
            var command = new CreateEventCommand(
                "Software Architecture Seminar",
                "Seminar about modern web architecture.",
                startsAtUtc,
                startsAtUtc.AddHours(4),
                80);

            var eventId = await handler.Handle(command, CancellationToken.None);

            var eventItem = await DbContext.Events
                .FirstOrDefaultAsync(e => e.Id == eventId);

            Assert.NotNull(eventItem);
            Assert.Equal(command.Title, eventItem.Title);
            Assert.Equal(command.Description, eventItem.Description);
            Assert.Equal(command.Capacity, eventItem.Capacity);
            Assert.Equal(organizerUserId, eventItem.OrganizerUserId);
            Assert.Equal(EventStatus.Draft, eventItem.Status);
        }

        [Fact]
        public async Task Handle_WithoutAuthenticatedUser_ThrowsUnauthorizedException()
        {
            var handler = new CreateEventCommandHandler(
                DbContext,
                new TestCurrentUserService(null));

            var startsAtUtc = new DateTime(2026, 9, 1, 9, 0, 0, DateTimeKind.Utc);
            var command = new CreateEventCommand(
                "Software Architecture Seminar",
                "Seminar about modern web architecture.",
                startsAtUtc,
                startsAtUtc.AddHours(4),
                80);

            await Assert.ThrowsAsync<UnauthorizedException>(() =>
                handler.Handle(command, CancellationToken.None));
        }

        private sealed class TestCurrentUserService : ICurrentUserService
        {
            public TestCurrentUserService(Guid? userId)
            {
                UserId = userId;
            }

            public Guid? UserId { get; }

            public string? Email => null;

            public bool IsAuthenticated => UserId is not null;

            public IReadOnlyCollection<string> Roles => Array.Empty<string>();

            public bool IsInRole(string role) => false;
        }
    }
}
