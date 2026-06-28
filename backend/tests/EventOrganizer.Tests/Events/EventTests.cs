using EventOrganizer.Domain.Events;

namespace EventOrganizer.Tests.Events;

public sealed class EventTests
{
    [Fact]
    public void Create_WithValidData_CreatesDraftEvent()
    {
        var organizerUserId = Guid.NewGuid();
        var startsAtUtc = new DateTime(2026, 9, 1, 9, 0, 0, DateTimeKind.Utc);
        var endsAtUtc = startsAtUtc.AddHours(4);
        var createdAtUtc = new DateTime(2026, 8, 1, 12, 0, 0, DateTimeKind.Utc);

        var eventItem = Event.Create(
            "Software Architecture Seminar",
            "Seminar about modern web architecture.",
            startsAtUtc,
            endsAtUtc,
            80,
            organizerUserId,
            createdAtUtc);

        Assert.NotEqual(Guid.Empty, eventItem.Id);
        Assert.Equal("Software Architecture Seminar", eventItem.Title);
        Assert.Equal(EventStatus.Draft, eventItem.Status);
        Assert.Equal(organizerUserId, eventItem.OrganizerUserId);
        Assert.Equal(createdAtUtc, eventItem.CreatedAtUtc);
    }

    [Fact]
    public void Create_WhenEndDateIsBeforeStartDate_Throws()
    {
        var startsAtUtc = new DateTime(2026, 9, 1, 9, 0, 0, DateTimeKind.Utc);

        var act = () => Event.Create(
            "Invalid event",
            "Invalid schedule.",
            startsAtUtc,
            startsAtUtc.AddMinutes(-1),
            50,
            Guid.NewGuid(),
            DateTime.UtcNow);

        Assert.Throws<ArgumentException>(act);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Create_WhenCapacityIsNotPositive_Throws(int capacity)
    {
        var startsAtUtc = new DateTime(2026, 9, 1, 9, 0, 0, DateTimeKind.Utc);

        var act = () => Event.Create(
            "Invalid event",
            "Invalid capacity.",
            startsAtUtc,
            startsAtUtc.AddHours(2),
            capacity,
            Guid.NewGuid(),
            DateTime.UtcNow);

        Assert.Throws<ArgumentOutOfRangeException>(act);
    }

    [Fact]
    public void Publish_WhenEventIsDraft_ChangesStatusToPublished()
    {
        var eventItem = CreateEvent();
        var updatedAtUtc = new DateTime(2026, 8, 2, 12, 0, 0, DateTimeKind.Utc);

        eventItem.Publish(updatedAtUtc);

        Assert.Equal(EventStatus.Published, eventItem.Status);
        Assert.Equal(updatedAtUtc, eventItem.UpdatedAtUtc);
    }

    [Fact]
    public void Publish_WhenEventIsCancelled_Throws()
    {
        var eventItem = CreateEvent();
        eventItem.Cancel(DateTime.UtcNow);

        var act = () => eventItem.Publish(DateTime.UtcNow);

        Assert.Throws<InvalidOperationException>(act);
    }

    private static Event CreateEvent()
    {
        var startsAtUtc = new DateTime(2026, 9, 1, 9, 0, 0, DateTimeKind.Utc);

        return Event.Create(
            "Software Architecture Seminar",
            "Seminar about modern web architecture.",
            startsAtUtc,
            startsAtUtc.AddHours(4),
            80,
            Guid.NewGuid(),
            new DateTime(2026, 8, 1, 12, 0, 0, DateTimeKind.Utc));
    }
}
