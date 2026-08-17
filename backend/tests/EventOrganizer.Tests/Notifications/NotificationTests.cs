using EventOrganizer.Domain.Notifications;

namespace EventOrganizer.Tests.Notifications;

public sealed class NotificationTests
{
    private static readonly DateTime CreatedAtUtc =
        new(2026, 8, 17, 10, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Create_WithValidData_CreatesUnreadNotification()
    {
        var recipientUserId = Guid.NewGuid();
        var relatedEntityId = Guid.NewGuid();

        var notification = Notification.Create(
            recipientUserId,
            NotificationType.BookingApproved,
            " Booking approved ",
            " Your resource booking has been approved. ",
            CreatedAtUtc,
            NotificationRelatedEntityType.EventResourceBooking,
            relatedEntityId);

        Assert.NotEqual(Guid.Empty, notification.Id);
        Assert.Equal(recipientUserId, notification.RecipientUserId);
        Assert.Equal(NotificationType.BookingApproved, notification.Type);
        Assert.Equal("Booking approved", notification.Title);
        Assert.Equal("Your resource booking has been approved.", notification.Message);
        Assert.Equal(NotificationRelatedEntityType.EventResourceBooking, notification.RelatedEntityType);
        Assert.Equal(relatedEntityId, notification.RelatedEntityId);
        Assert.Equal(CreatedAtUtc, notification.CreatedAtUtc);
        Assert.Null(notification.ReadAtUtc);
        Assert.False(notification.IsRead);
        Assert.Equal(1, notification.Version);
    }

    [Fact]
    public void Create_WithoutRelatedEntity_AllowsNotificationWithoutNavigationTarget()
    {
        var notification = CreateNotification();

        Assert.Null(notification.RelatedEntityType);
        Assert.Null(notification.RelatedEntityId);
    }

    [Fact]
    public void Create_WhenRecipientUserIdIsEmpty_Throws()
    {
        var act = () => Notification.Create(
            Guid.Empty,
            NotificationType.EventCancelled,
            "Event cancelled",
            "The event has been cancelled.",
            CreatedAtUtc);

        Assert.Throws<ArgumentException>(act);
    }

    [Fact]
    public void Create_WhenNotificationTypeIsInvalid_Throws()
    {
        var act = () => Notification.Create(
            Guid.NewGuid(),
            (NotificationType)999,
            "Event cancelled",
            "The event has been cancelled.",
            CreatedAtUtc);

        Assert.Throws<ArgumentOutOfRangeException>(act);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WhenTitleIsMissing_Throws(string? title)
    {
        var act = () => Notification.Create(
            Guid.NewGuid(),
            NotificationType.EventCancelled,
            title!,
            "The event has been cancelled.",
            CreatedAtUtc);

        Assert.Throws<ArgumentException>(act);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WhenMessageIsMissing_Throws(string? message)
    {
        var act = () => Notification.Create(
            Guid.NewGuid(),
            NotificationType.EventCancelled,
            "Event cancelled",
            message!,
            CreatedAtUtc);

        Assert.Throws<ArgumentException>(act);
    }

    [Fact]
    public void Create_WhenTitleExceedsMaximumLength_Throws()
    {
        var act = () => Notification.Create(
            Guid.NewGuid(),
            NotificationType.EventCancelled,
            new string('a', Notification.MaxTitleLength + 1),
            "The event has been cancelled.",
            CreatedAtUtc);

        Assert.Throws<ArgumentException>(act);
    }

    [Fact]
    public void Create_WhenMessageExceedsMaximumLength_Throws()
    {
        var act = () => Notification.Create(
            Guid.NewGuid(),
            NotificationType.EventCancelled,
            "Event cancelled",
            new string('a', Notification.MaxMessageLength + 1),
            CreatedAtUtc);

        Assert.Throws<ArgumentException>(act);
    }

    [Fact]
    public void Create_WhenOnlyRelatedEntityTypeIsProvided_Throws()
    {
        var act = () => Notification.Create(
            Guid.NewGuid(),
            NotificationType.EventCancelled,
            "Event cancelled",
            "The event has been cancelled.",
            CreatedAtUtc,
            NotificationRelatedEntityType.Event);

        Assert.Throws<ArgumentException>(act);
    }

    [Fact]
    public void Create_WhenOnlyRelatedEntityIdIsProvided_Throws()
    {
        var act = () => Notification.Create(
            Guid.NewGuid(),
            NotificationType.EventCancelled,
            "Event cancelled",
            "The event has been cancelled.",
            CreatedAtUtc,
            relatedEntityId: Guid.NewGuid());

        Assert.Throws<ArgumentException>(act);
    }

    [Fact]
    public void Create_WhenRelatedEntityIdIsEmpty_Throws()
    {
        var act = () => Notification.Create(
            Guid.NewGuid(),
            NotificationType.EventCancelled,
            "Event cancelled",
            "The event has been cancelled.",
            CreatedAtUtc,
            NotificationRelatedEntityType.Event,
            Guid.Empty);

        Assert.Throws<ArgumentException>(act);
    }

    [Fact]
    public void Create_WhenRelatedEntityTypeIsInvalid_Throws()
    {
        var act = () => Notification.Create(
            Guid.NewGuid(),
            NotificationType.EventCancelled,
            "Event cancelled",
            "The event has been cancelled.",
            CreatedAtUtc,
            (NotificationRelatedEntityType)999,
            Guid.NewGuid());

        Assert.Throws<ArgumentOutOfRangeException>(act);
    }

    [Fact]
    public void MarkAsRead_WhenUnread_StoresReadTimeAndIncrementsVersion()
    {
        var notification = CreateNotification();
        var readAtUtc = CreatedAtUtc.AddMinutes(5);

        notification.MarkAsRead(readAtUtc);

        Assert.True(notification.IsRead);
        Assert.Equal(readAtUtc, notification.ReadAtUtc);
        Assert.Equal(2, notification.Version);
    }

    [Fact]
    public void MarkAsRead_WhenAlreadyRead_IsIdempotent()
    {
        var notification = CreateNotification();
        var originalReadAtUtc = CreatedAtUtc.AddMinutes(5);
        notification.MarkAsRead(originalReadAtUtc);

        notification.MarkAsRead(CreatedAtUtc.AddMinutes(10));

        Assert.Equal(originalReadAtUtc, notification.ReadAtUtc);
        Assert.Equal(2, notification.Version);
    }

    [Fact]
    public void MarkAsRead_WhenReadTimePrecedesCreation_Throws()
    {
        var notification = CreateNotification();

        var act = () => notification.MarkAsRead(CreatedAtUtc.AddSeconds(-1));

        Assert.Throws<ArgumentException>(act);
    }

    private static Notification CreateNotification()
    {
        return Notification.Create(
            Guid.NewGuid(),
            NotificationType.EventCancelled,
            "Event cancelled",
            "The event has been cancelled.",
            CreatedAtUtc);
    }
}
