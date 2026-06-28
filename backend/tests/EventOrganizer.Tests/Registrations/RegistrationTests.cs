using EventOrganizer.Domain.Registrations;

namespace EventOrganizer.Tests.Registrations;

public sealed class RegistrationTests
{
    [Fact]
    public void Create_WithValidData_CreatesPendingRegistration()
    {
        var eventId = Guid.NewGuid();
        var participantUserId = Guid.NewGuid();
        var createdAtUtc = new DateTime(2026, 8, 1, 12, 0, 0, DateTimeKind.Utc);

        var registration = Registration.Create(eventId, participantUserId, createdAtUtc);

        Assert.NotEqual(Guid.Empty, registration.Id);
        Assert.Equal(eventId, registration.EventId);
        Assert.Equal(participantUserId, registration.ParticipantUserId);
        Assert.Equal(RegistrationStatus.Pending, registration.Status);
        Assert.Equal(createdAtUtc, registration.CreatedAtUtc);
    }

    [Fact]
    public void Confirm_WhenRegistrationIsPending_ChangesStatus()
    {
        var registration = CreateRegistration();

        registration.Confirm(DateTime.UtcNow);

        Assert.Equal(RegistrationStatus.Confirmed, registration.Status);
    }

    [Fact]
    public void Reject_WhenRegistrationIsConfirmed_Throws()
    {
        var registration = CreateRegistration();
        registration.Confirm(DateTime.UtcNow);

        var act = () => registration.Reject(DateTime.UtcNow);

        Assert.Throws<InvalidOperationException>(act);
    }

    [Fact]
    public void Create_WhenParticipantUserIdIsEmpty_Throws()
    {
        var act = () => Registration.Create(Guid.NewGuid(), Guid.Empty, DateTime.UtcNow);

        Assert.Throws<ArgumentException>(act);
    }

    private static Registration CreateRegistration()
    {
        return Registration.Create(Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow);
    }
}
