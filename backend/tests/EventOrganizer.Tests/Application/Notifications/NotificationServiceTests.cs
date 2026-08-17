using EventOrganizer.Application.Notifications;
using EventOrganizer.Domain.Notifications;
using Microsoft.EntityFrameworkCore;

namespace EventOrganizer.Tests.Application.Notifications
{
    public sealed class NotificationServiceTests : ApplicationTestBase
    {
        [Fact]
        public async Task AddEventCancelled_WithDuplicateRecipients_CreatesOneNotificationPerUser()
        {
            var recipientUserId = await CreateOrganizerUserAsync();
            var eventId = Guid.NewGuid();
            var service = new NotificationService(DbContext);

            service.AddEventCancelled(
                [recipientUserId, recipientUserId],
                eventId,
                "Architecture Conference",
                DateTime.UtcNow);
            await DbContext.SaveChangesAsync();

            var notification = await DbContext.Notifications.SingleAsync();
            Assert.Equal(recipientUserId, notification.RecipientUserId);
            Assert.Equal(NotificationType.EventCancelled, notification.Type);
            Assert.Equal(eventId, notification.RelatedEntityId);
        }

        [Fact]
        public async Task AddBookingRejected_WithoutReason_CreatesCleanMessage()
        {
            var recipientUserId = await CreateOrganizerUserAsync();
            var service = new NotificationService(DbContext);

            service.AddBookingRejected(
                recipientUserId,
                Guid.NewGuid(),
                "Architecture Conference",
                null,
                DateTime.UtcNow);
            await DbContext.SaveChangesAsync();

            var notification = await DbContext.Notifications.SingleAsync();
            Assert.Equal(NotificationType.BookingRejected, notification.Type);
            Assert.DoesNotContain("Razlog:", notification.Message);
        }
    }
}
