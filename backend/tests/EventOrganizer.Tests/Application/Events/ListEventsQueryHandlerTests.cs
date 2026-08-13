using EventOrganizer.Application.Common.Constants;
using EventOrganizer.Application.Common.Exceptions;
using EventOrganizer.Application.Common.Interfaces;
using EventOrganizer.Application.Queries.ListEvents;

namespace EventOrganizer.Tests.Application.Events
{
    public sealed class ListEventsQueryHandlerTests : ApplicationTestBase
    {
        [Fact]
        public async Task Handle_WithOrganizer_ReturnsOwnEventsOrderedByStartDate()
        {
            var organizerUserId = await CreateOrganizerUserAsync();
            var otherOrganizerUserId = await CreateOrganizerUserAsync("other@example.com");
            var laterStart = new DateTime(2026, 10, 1, 9, 0, 0, DateTimeKind.Utc);
            var earlierStart = new DateTime(2026, 9, 1, 9, 0, 0, DateTimeKind.Utc);

            var laterEvent = await CreateEventAsync(
                organizerUserId,
                title: "Later event",
                startsAtUtc: laterStart);

            var earlierEvent = await CreateEventAsync(
                organizerUserId,
                title: "Earlier event",
                startsAtUtc: earlierStart);

            await CreateEventAsync(
                otherOrganizerUserId,
                title: "Other organizer event");

            var handler = new ListEventsQueryHandler(
                DbContext,
                new TestCurrentUserService(organizerUserId, ApplicationRoles.Organizer));

            var result = await handler.Handle(
                new ListEventsQuery(),
                CancellationToken.None);

            Assert.Equal(2, result.Count);
            Assert.Equal(earlierEvent.Id, result[0].Id);
            Assert.Equal(laterEvent.Id, result[1].Id);
        }

        [Fact]
        public async Task Handle_WithAdmin_ReturnsAllEvents()
        {
            await CreateEventAsync(title: "First event");
            await CreateEventAsync(title: "Second event");
            var handler = new ListEventsQueryHandler(
                DbContext,
                new TestCurrentUserService(Guid.NewGuid(), ApplicationRoles.Admin));

            var result = await handler.Handle(
                new ListEventsQuery(),
                CancellationToken.None);

            Assert.Equal(2, result.Count);
        }

        [Fact]
        public async Task Handle_WithParticipant_ThrowsForbiddenException()
        {
            var handler = new ListEventsQueryHandler(
                DbContext,
                new TestCurrentUserService(Guid.NewGuid(), ApplicationRoles.Participant));

            await Assert.ThrowsAsync<ForbiddenException>(() =>
                handler.Handle(new ListEventsQuery(), CancellationToken.None));
        }

        private sealed class TestCurrentUserService : ICurrentUserService
        {
            public TestCurrentUserService(Guid? userId, params string[] roles)
            {
                UserId = userId;
                Roles = roles;
            }

            public Guid? UserId { get; }

            public string? Email => null;

            public bool IsAuthenticated => UserId is not null;

            public IReadOnlyCollection<string> Roles { get; }

            public bool IsInRole(string role)
            {
                return Roles.Contains(role);
            }
        }
    }
}
