using EventOrganizer.Application.Commands.MarkAllNotificationsAsRead;
using EventOrganizer.Application.Commands.MarkNotificationAsRead;
using EventOrganizer.Application.Common.Exceptions;
using EventOrganizer.Application.Common.Interfaces;
using EventOrganizer.Application.Queries.GetUnreadNotificationCount;
using EventOrganizer.Application.Queries.ListMyNotifications;
using EventOrganizer.Domain.Notifications;
using Microsoft.EntityFrameworkCore;

namespace EventOrganizer.Tests.Application.Notifications
{
    public sealed class NotificationApplicationTests : ApplicationTestBase
    {
        [Fact]
        public async Task ListMine_ReturnsLatestFiftyNotificationsForCurrentUser()
        {
            var currentUserId = await CreateOrganizerUserAsync();
            var otherUserId = await CreateOrganizerUserAsync("notification-other@example.com");
            var createdAtUtc = DateTime.UtcNow.AddHours(-2);
            var currentUserNotifications = Enumerable.Range(0, 55)
                .Select(index => CreateNotification(
                    currentUserId,
                    createdAtUtc.AddMinutes(index),
                    $"Notification {index}"))
                .ToArray();
            DbContext.Notifications.AddRange(currentUserNotifications);
            DbContext.Notifications.Add(CreateNotification(
                otherUserId,
                createdAtUtc.AddHours(3),
                "Other user's notification"));
            await DbContext.SaveChangesAsync();
            var handler = new ListMyNotificationsQueryHandler(
                DbContext,
                new TestCurrentUserService(currentUserId));

            var result = await handler.Handle(
                new ListMyNotificationsQuery(),
                CancellationToken.None);

            Assert.Equal(ListMyNotificationsQueryHandler.NotificationLimit, result.Count);
            Assert.Equal("Notification 54", result[0].Title);
            Assert.Equal("Notification 5", result[^1].Title);
            Assert.DoesNotContain(result, notification =>
                notification.Title == "Other user's notification");
        }

        [Fact]
        public async Task ListMine_MapsNotificationResponse()
        {
            var currentUserId = await CreateOrganizerUserAsync();
            var eventId = Guid.NewGuid();
            var notification = Notification.Create(
                currentUserId,
                NotificationType.EventCancelled,
                "Event cancelled",
                "The event has been cancelled.",
                DateTime.UtcNow.AddMinutes(-1),
                NotificationRelatedEntityType.Event,
                eventId);
            DbContext.Notifications.Add(notification);
            await DbContext.SaveChangesAsync();
            var handler = new ListMyNotificationsQueryHandler(
                DbContext,
                new TestCurrentUserService(currentUserId));

            var result = await handler.Handle(
                new ListMyNotificationsQuery(),
                CancellationToken.None);

            var response = Assert.Single(result);
            Assert.Equal(notification.Id, response.Id);
            Assert.Equal("EventCancelled", response.Type);
            Assert.Equal("Event", response.RelatedEntityType);
            Assert.Equal(eventId, response.RelatedEntityId);
            Assert.False(response.IsRead);
        }

        [Fact]
        public async Task GetUnreadCount_CountsOnlyUnreadNotificationsForCurrentUser()
        {
            var currentUserId = await CreateOrganizerUserAsync();
            var otherUserId = await CreateOrganizerUserAsync("count-other@example.com");
            var readNotification = CreateNotification(currentUserId, DateTime.UtcNow.AddMinutes(-3));
            readNotification.MarkAsRead(DateTime.UtcNow.AddMinutes(-2));
            DbContext.Notifications.AddRange(
                CreateNotification(currentUserId, DateTime.UtcNow.AddMinutes(-2)),
                CreateNotification(currentUserId, DateTime.UtcNow.AddMinutes(-1)),
                readNotification,
                CreateNotification(otherUserId, DateTime.UtcNow.AddMinutes(-1)));
            await DbContext.SaveChangesAsync();
            var handler = new GetUnreadNotificationCountQueryHandler(
                DbContext,
                new TestCurrentUserService(currentUserId));

            var result = await handler.Handle(
                new GetUnreadNotificationCountQuery(),
                CancellationToken.None);

            Assert.Equal(2, result.UnreadCount);
        }

        [Fact]
        public async Task MarkAsRead_ForOwnedNotification_MarksNotificationAsRead()
        {
            var currentUserId = await CreateOrganizerUserAsync();
            var notification = CreateNotification(currentUserId, DateTime.UtcNow.AddMinutes(-1));
            DbContext.Notifications.Add(notification);
            await DbContext.SaveChangesAsync();
            var handler = new MarkNotificationAsReadCommandHandler(
                DbContext,
                new TestCurrentUserService(currentUserId));

            await handler.Handle(
                new MarkNotificationAsReadCommand(notification.Id),
                CancellationToken.None);

            Assert.True(notification.IsRead);
            Assert.Equal(2, notification.Version);
        }

        [Fact]
        public async Task MarkAsRead_WhenAlreadyRead_IsIdempotent()
        {
            var currentUserId = await CreateOrganizerUserAsync();
            var notification = CreateNotification(currentUserId, DateTime.UtcNow.AddMinutes(-2));
            notification.MarkAsRead(DateTime.UtcNow.AddMinutes(-1));
            DbContext.Notifications.Add(notification);
            await DbContext.SaveChangesAsync();
            var originalReadAtUtc = notification.ReadAtUtc;
            var handler = new MarkNotificationAsReadCommandHandler(
                DbContext,
                new TestCurrentUserService(currentUserId));

            await handler.Handle(
                new MarkNotificationAsReadCommand(notification.Id),
                CancellationToken.None);

            Assert.Equal(originalReadAtUtc, notification.ReadAtUtc);
            Assert.Equal(2, notification.Version);
        }

        [Fact]
        public async Task MarkAsRead_ForAnotherUsersNotification_ThrowsNotFound()
        {
            var currentUserId = await CreateOrganizerUserAsync();
            var otherUserId = await CreateOrganizerUserAsync("mark-other@example.com");
            var notification = CreateNotification(otherUserId, DateTime.UtcNow.AddMinutes(-1));
            DbContext.Notifications.Add(notification);
            await DbContext.SaveChangesAsync();
            var handler = new MarkNotificationAsReadCommandHandler(
                DbContext,
                new TestCurrentUserService(currentUserId));

            var action = () => handler.Handle(
                new MarkNotificationAsReadCommand(notification.Id),
                CancellationToken.None);

            await Assert.ThrowsAsync<NotFoundException>(action);
            Assert.False(notification.IsRead);
        }

        [Fact]
        public async Task MarkAllAsRead_MarksOnlyCurrentUsersUnreadNotifications()
        {
            var currentUserId = await CreateOrganizerUserAsync();
            var otherUserId = await CreateOrganizerUserAsync("mark-all-other@example.com");
            var first = CreateNotification(currentUserId, DateTime.UtcNow.AddMinutes(-3));
            var second = CreateNotification(currentUserId, DateTime.UtcNow.AddMinutes(-2));
            var alreadyRead = CreateNotification(currentUserId, DateTime.UtcNow.AddMinutes(-4));
            alreadyRead.MarkAsRead(DateTime.UtcNow.AddMinutes(-3));
            var originalReadAtUtc = alreadyRead.ReadAtUtc;
            var otherUsersNotification = CreateNotification(
                otherUserId,
                DateTime.UtcNow.AddMinutes(-1));
            DbContext.Notifications.AddRange(
                first,
                second,
                alreadyRead,
                otherUsersNotification);
            await DbContext.SaveChangesAsync();
            var handler = new MarkAllNotificationsAsReadCommandHandler(
                DbContext,
                new TestCurrentUserService(currentUserId));

            await handler.Handle(
                new MarkAllNotificationsAsReadCommand(),
                CancellationToken.None);

            Assert.True(first.IsRead);
            Assert.True(second.IsRead);
            Assert.Equal(first.ReadAtUtc, second.ReadAtUtc);
            Assert.Equal(originalReadAtUtc, alreadyRead.ReadAtUtc);
            Assert.False(otherUsersNotification.IsRead);
        }

        [Fact]
        public async Task ListMine_WhenCurrentUserIsNotAuthenticated_ThrowsUnauthorized()
        {
            var handler = new ListMyNotificationsQueryHandler(
                DbContext,
                new TestCurrentUserService(null));

            var action = () => handler.Handle(
                new ListMyNotificationsQuery(),
                CancellationToken.None);

            await Assert.ThrowsAsync<UnauthorizedException>(action);
        }

        private static Notification CreateNotification(
            Guid recipientUserId,
            DateTime createdAtUtc,
            string title = "Event cancelled")
        {
            return Notification.Create(
                recipientUserId,
                NotificationType.EventCancelled,
                title,
                "The event has been cancelled.",
                createdAtUtc);
        }

        private sealed class TestCurrentUserService : ICurrentUserService
        {
            public TestCurrentUserService(Guid? userId)
            {
                UserId = userId;
            }

            public Guid? UserId { get; }

            public string? Email => UserId.HasValue ? "notification-user@example.com" : null;

            public bool IsAuthenticated => UserId.HasValue;

            public IReadOnlyCollection<string> Roles => [];

            public bool IsInRole(string role) => false;
        }
    }
}
