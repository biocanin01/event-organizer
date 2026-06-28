using EventOrganizer.Domain.Resources;

namespace EventOrganizer.Tests.Resources;

public sealed class ResourceReservationTests
{
    [Fact]
    public void Create_WithValidData_CreatesPendingReservation()
    {
        var eventId = Guid.NewGuid();
        var resourceId = Guid.NewGuid();
        var startsAtUtc = new DateTime(2026, 9, 1, 9, 0, 0, DateTimeKind.Utc);

        var reservation = ResourceReservation.Create(
            eventId,
            resourceId,
            startsAtUtc,
            startsAtUtc.AddHours(2),
            DateTime.UtcNow);

        Assert.NotEqual(Guid.Empty, reservation.Id);
        Assert.Equal(eventId, reservation.EventId);
        Assert.Equal(resourceId, reservation.ResourceId);
        Assert.Equal(ResourceReservationStatus.Pending, reservation.Status);
    }

    [Fact]
    public void Confirm_WhenReservationIsPending_ChangesStatus()
    {
        var reservation = CreateReservation();

        reservation.Confirm(DateTime.UtcNow);

        Assert.Equal(ResourceReservationStatus.Confirmed, reservation.Status);
    }

    [Fact]
    public void Reject_WhenReservationIsConfirmed_Throws()
    {
        var reservation = CreateReservation();
        reservation.Confirm(DateTime.UtcNow);

        var act = () => reservation.Reject(DateTime.UtcNow);

        Assert.Throws<InvalidOperationException>(act);
    }

    private static ResourceReservation CreateReservation()
    {
        var startsAtUtc = new DateTime(2026, 9, 1, 9, 0, 0, DateTimeKind.Utc);

        return ResourceReservation.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            startsAtUtc,
            startsAtUtc.AddHours(2),
            DateTime.UtcNow);
    }
}
