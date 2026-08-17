using EventOrganizer.Application.Common.Constants;
using EventOrganizer.Domain.Notifications;
using EventOrganizer.Domain.Users;
using EventOrganizer.Infrastructure.Identity;
using EventOrganizer.Infrastructure.Persistance;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Text.Json;

namespace EventOrganizer.Tests.Api
{
    public sealed class NotificationEndpointTests
        : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly CustomWebApplicationFactory _factory;

        public NotificationEndpointTests(CustomWebApplicationFactory factory)
        {
            _factory = factory;
        }

        [Theory]
        [InlineData("GET", "/api/notifications")]
        [InlineData("GET", "/api/notifications/unread-count")]
        [InlineData("PATCH", "/api/notifications/00000000-0000-0000-0000-000000000001/read")]
        [InlineData("PATCH", "/api/notifications/read-all")]
        public async Task Endpoint_WhenUnauthenticated_ReturnsUnauthorized(
            string method,
            string path)
        {
            var client = _factory.CreateClient();
            using var request = new HttpRequestMessage(new HttpMethod(method), path);

            var response = await client.SendAsync(request);

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task ListMine_ReturnsOnlyCurrentUsersNotificationsInDescendingOrder()
        {
            var currentUserId = Guid.NewGuid();
            var otherUserId = Guid.NewGuid();
            var older = await SeedNotificationAsync(
                currentUserId,
                DateTime.UtcNow.AddMinutes(-2),
                "Older notification",
                additionalUserId: otherUserId);
            var newer = await SeedNotificationAsync(
                currentUserId,
                DateTime.UtcNow.AddMinutes(-1),
                "Newer notification");
            await SeedNotificationAsync(
                otherUserId,
                DateTime.UtcNow,
                "Other user's notification");
            var client = CreateAuthenticatedClient(currentUserId);

            var response = await client.GetAsync("/api/notifications");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            using var payload = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            var notifications = payload.RootElement.EnumerateArray().ToArray();
            Assert.Equal(2, notifications.Length);
            Assert.Equal(newer, notifications[0].GetProperty("id").GetGuid());
            Assert.Equal(older, notifications[1].GetProperty("id").GetGuid());
            Assert.Equal("EventCancelled", notifications[0].GetProperty("type").GetString());
            Assert.False(notifications[0].GetProperty("isRead").GetBoolean());
        }

        [Fact]
        public async Task GetUnreadCount_ReturnsCurrentUsersUnreadCount()
        {
            var currentUserId = Guid.NewGuid();
            var otherUserId = Guid.NewGuid();
            await SeedNotificationAsync(
                currentUserId,
                DateTime.UtcNow.AddMinutes(-2),
                "First unread",
                additionalUserId: otherUserId);
            await SeedNotificationAsync(
                currentUserId,
                DateTime.UtcNow.AddMinutes(-1),
                "Second unread");
            await SeedNotificationAsync(
                currentUserId,
                DateTime.UtcNow.AddMinutes(-3),
                "Already read",
                isRead: true);
            await SeedNotificationAsync(
                otherUserId,
                DateTime.UtcNow,
                "Other user's unread");
            var client = CreateAuthenticatedClient(currentUserId);

            var response = await client.GetAsync("/api/notifications/unread-count");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            using var payload = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            Assert.Equal(2, payload.RootElement.GetProperty("unreadCount").GetInt32());
        }

        [Fact]
        public async Task MarkAsRead_ForOwnedNotification_ReturnsNoContentAndPersistsReadState()
        {
            var currentUserId = Guid.NewGuid();
            var notificationId = await SeedNotificationAsync(
                currentUserId,
                DateTime.UtcNow.AddMinutes(-1),
                "Unread notification");
            var client = CreateAuthenticatedClient(currentUserId);

            var response = await client.PatchAsync(
                $"/api/notifications/{notificationId}/read",
                null);

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            var persistedNotification = await FindNotificationAsync(notificationId);
            Assert.True(persistedNotification.IsRead);
            Assert.Equal(2, persistedNotification.Version);
        }

        [Fact]
        public async Task MarkAsRead_WhenRepeated_RemainsSuccessfulAndIdempotent()
        {
            var currentUserId = Guid.NewGuid();
            var notificationId = await SeedNotificationAsync(
                currentUserId,
                DateTime.UtcNow.AddMinutes(-1),
                "Unread notification");
            var client = CreateAuthenticatedClient(currentUserId);

            var firstResponse = await client.PatchAsync(
                $"/api/notifications/{notificationId}/read",
                null);
            var firstReadAtUtc = (await FindNotificationAsync(notificationId)).ReadAtUtc;
            var secondResponse = await client.PatchAsync(
                $"/api/notifications/{notificationId}/read",
                null);
            var persistedNotification = await FindNotificationAsync(notificationId);

            Assert.Equal(HttpStatusCode.NoContent, firstResponse.StatusCode);
            Assert.Equal(HttpStatusCode.NoContent, secondResponse.StatusCode);
            Assert.Equal(firstReadAtUtc, persistedNotification.ReadAtUtc);
            Assert.Equal(2, persistedNotification.Version);
        }

        [Fact]
        public async Task MarkAsRead_ForAnotherUsersNotification_ReturnsNotFound()
        {
            var currentUserId = Guid.NewGuid();
            var otherUserId = Guid.NewGuid();
            var notificationId = await SeedNotificationAsync(
                otherUserId,
                DateTime.UtcNow.AddMinutes(-1),
                "Other user's notification",
                additionalUserId: currentUserId);
            var client = CreateAuthenticatedClient(currentUserId);

            var response = await client.PatchAsync(
                $"/api/notifications/{notificationId}/read",
                null);

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
            Assert.False((await FindNotificationAsync(notificationId)).IsRead);
        }

        [Fact]
        public async Task MarkAllAsRead_MarksOnlyCurrentUsersNotifications()
        {
            var currentUserId = Guid.NewGuid();
            var otherUserId = Guid.NewGuid();
            var firstId = await SeedNotificationAsync(
                currentUserId,
                DateTime.UtcNow.AddMinutes(-2),
                "First unread",
                additionalUserId: otherUserId);
            var secondId = await SeedNotificationAsync(
                currentUserId,
                DateTime.UtcNow.AddMinutes(-1),
                "Second unread");
            var otherUsersNotificationId = await SeedNotificationAsync(
                otherUserId,
                DateTime.UtcNow,
                "Other user's unread");
            var client = CreateAuthenticatedClient(currentUserId);

            var response = await client.PatchAsync("/api/notifications/read-all", null);

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            Assert.True((await FindNotificationAsync(firstId)).IsRead);
            Assert.True((await FindNotificationAsync(secondId)).IsRead);
            Assert.False((await FindNotificationAsync(otherUsersNotificationId)).IsRead);
        }

        private async Task<Guid> SeedNotificationAsync(
            Guid recipientUserId,
            DateTime createdAtUtc,
            string title,
            bool isRead = false,
            Guid? additionalUserId = null)
        {
            using var scope = _factory.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await EnsureUserExistsAsync(dbContext, recipientUserId);
            if (additionalUserId.HasValue)
            {
                await EnsureUserExistsAsync(dbContext, additionalUserId.Value);
            }

            var notification = Notification.Create(
                recipientUserId,
                NotificationType.EventCancelled,
                title,
                "The event has been cancelled.",
                createdAtUtc);
            if (isRead)
            {
                notification.MarkAsRead(createdAtUtc.AddSeconds(1));
            }

            dbContext.Notifications.Add(notification);
            await dbContext.SaveChangesAsync();
            return notification.Id;
        }

        private async Task<Notification> FindNotificationAsync(Guid notificationId)
        {
            using var scope = _factory.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            return await dbContext.Notifications
                .AsNoTracking()
                .SingleAsync(notification => notification.Id == notificationId);
        }

        private static async Task EnsureUserExistsAsync(
            AppDbContext dbContext,
            Guid userId)
        {
            if (await dbContext.Users.AnyAsync(user => user.Id == userId))
            {
                return;
            }

            dbContext.Users.Add(new ApplicationUser
            {
                Id = userId,
                UserName = $"{userId:N}@example.com",
                Email = $"{userId:N}@example.com",
                FullName = "Notification User",
                Status = UserStatus.Active,
                CreatedAtUtc = DateTime.UtcNow,
            });
        }

        private HttpClient CreateAuthenticatedClient(Guid userId)
        {
            var client = _factory.CreateClient();
            client.DefaultRequestHeaders.Add(TestAuthHandler.UserIdHeader, userId.ToString());
            client.DefaultRequestHeaders.Add(
                TestAuthHandler.RoleHeader,
                ApplicationRoles.Participant);
            return client;
        }
    }
}
