using EventOrganizer.Domain.Notifications;
using Microsoft.EntityFrameworkCore;

namespace EventOrganizer.Tests.Application.Notifications
{
    public sealed class NotificationPersistenceTests : ApplicationTestBase
    {
        [Fact]
        public async Task SaveChanges_WithValidNotification_PersistsNotification()
        {
            var recipientUserId = await CreateOrganizerUserAsync();
            var relatedEntityId = Guid.NewGuid();
            var notification = Notification.Create(
                recipientUserId,
                NotificationType.BookingRejected,
                "Booking rejected",
                "The resource booking was rejected.",
                DateTime.UtcNow,
                NotificationRelatedEntityType.EventResourceBooking,
                relatedEntityId);
            DbContext.Notifications.Add(notification);

            await DbContext.SaveChangesAsync();
            DbContext.ChangeTracker.Clear();

            var persistedNotification = await DbContext.Notifications
                .SingleAsync(item => item.Id == notification.Id);
            Assert.Equal(recipientUserId, persistedNotification.RecipientUserId);
            Assert.Equal(NotificationType.BookingRejected, persistedNotification.Type);
            Assert.Equal(NotificationRelatedEntityType.EventResourceBooking, persistedNotification.RelatedEntityType);
            Assert.Equal(relatedEntityId, persistedNotification.RelatedEntityId);
            Assert.False(persistedNotification.IsRead);
        }

        [Fact]
        public async Task SaveChanges_WhenRecipientDoesNotExist_Throws()
        {
            DbContext.Notifications.Add(CreateNotification(Guid.NewGuid()));

            await Assert.ThrowsAsync<DbUpdateException>(() =>
                DbContext.SaveChangesAsync());
        }

        [Fact]
        public async Task SaveChanges_WhenRecipientWithNotificationIsDeleted_Throws()
        {
            var recipientUserId = await CreateOrganizerUserAsync();
            DbContext.Notifications.Add(CreateNotification(recipientUserId));
            await DbContext.SaveChangesAsync();
            DbContext.ChangeTracker.Clear();
            var recipient = await DbContext.Users.SingleAsync(user => user.Id == recipientUserId);
            DbContext.Users.Remove(recipient);

            await Assert.ThrowsAsync<DbUpdateException>(() =>
                DbContext.SaveChangesAsync());
        }

        [Fact]
        public async Task SaveChanges_WithStaleNotificationVersion_ThrowsConcurrencyException()
        {
            var recipientUserId = await CreateOrganizerUserAsync();
            var notification = CreateNotification(recipientUserId);
            DbContext.Notifications.Add(notification);
            await DbContext.SaveChangesAsync();
            DbContext.ChangeTracker.Clear();

            using var secondContext = CreateDbContext();
            var firstCopy = await DbContext.Notifications.SingleAsync(item => item.Id == notification.Id);
            var secondCopy = await secondContext.Notifications.SingleAsync(item => item.Id == notification.Id);
            firstCopy.MarkAsRead(DateTime.UtcNow);
            secondCopy.MarkAsRead(DateTime.UtcNow.AddSeconds(1));

            await DbContext.SaveChangesAsync();

            await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() =>
                secondContext.SaveChangesAsync());
        }

        [Fact]
        public void Model_ContainsExpectedNotificationConfiguration()
        {
            var entityType = DbContext.Model.FindEntityType(typeof(Notification));

            Assert.NotNull(entityType);
            Assert.Equal(
                Notification.MaxTitleLength,
                entityType.FindProperty(nameof(Notification.Title))!.GetMaxLength());
            Assert.Equal(
                Notification.MaxMessageLength,
                entityType.FindProperty(nameof(Notification.Message))!.GetMaxLength());
            Assert.Equal(
                typeof(string),
                entityType.FindProperty(nameof(Notification.Type))!
                    .GetTypeMapping()
                    .Converter!
                    .ProviderClrType);
            Assert.Equal(
                typeof(string),
                entityType.FindProperty(nameof(Notification.RelatedEntityType))!
                    .GetTypeMapping()
                    .Converter!
                    .ProviderClrType);
            Assert.True(entityType.FindProperty(nameof(Notification.Version))!.IsConcurrencyToken);
            Assert.Null(entityType.FindProperty(nameof(Notification.IsRead)));
            Assert.Contains(entityType.GetIndexes(), index =>
                index.Properties.Select(property => property.Name).SequenceEqual(new[]
                {
                    nameof(Notification.RecipientUserId),
                    nameof(Notification.CreatedAtUtc),
                }));
            Assert.Contains(entityType.GetIndexes(), index =>
                index.GetDatabaseName() == "IX_Notifications_RecipientUserId_Unread"
                && index.GetFilter() == "\"ReadAtUtc\" IS NULL");
        }

        private static Notification CreateNotification(Guid recipientUserId)
        {
            return Notification.Create(
                recipientUserId,
                NotificationType.EventCancelled,
                "Event cancelled",
                "The event has been cancelled.",
                DateTime.UtcNow);
        }
    }
}
