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
        Assert.Equal(1, registration.Version);
        Assert.Equal(createdAtUtc, registration.CreatedAtUtc);
    }

    [Fact]
    public void Confirm_WhenRegistrationIsPending_ChangesStatus()
    {
        var registration = CreateRegistration();

        var adminUserId = Guid.NewGuid();
        var decidedAtUtc = DateTime.UtcNow;

        registration.Confirm(adminUserId, decidedAtUtc);

        Assert.Equal(RegistrationStatus.Confirmed, registration.Status);
        Assert.Equal(adminUserId, registration.DecidedByUserId);
        Assert.Equal(decidedAtUtc, registration.DecidedAtUtc);
        Assert.Equal(2, registration.Version);
    }

    [Fact]
    public void Reject_WhenRegistrationIsConfirmed_Throws()
    {
        var registration = CreateRegistration();
        registration.Confirm(Guid.NewGuid(), DateTime.UtcNow);

        var act = () => registration.Reject("No capacity.", Guid.NewGuid(), DateTime.UtcNow);

        Assert.Throws<InvalidOperationException>(act);
    }

    [Fact]
    public void Reject_WhenRegistrationIsPending_StoresDecision()
    {
        var registration = CreateRegistration();
        var adminUserId = Guid.NewGuid();

        registration.Reject(" Capacity reached. ", adminUserId, DateTime.UtcNow);

        Assert.Equal(RegistrationStatus.Rejected, registration.Status);
        Assert.Equal("Capacity reached.", registration.RejectionReason);
        Assert.Equal(adminUserId, registration.DecidedByUserId);
        Assert.Equal(2, registration.Version);
    }

    [Fact]
    public void Cancel_WhenRegistrationIsConfirmed_CancelsAndIncrementsVersion()
    {
        var registration = CreateRegistration();
        registration.Confirm(Guid.NewGuid(), DateTime.UtcNow);

        registration.Cancel(DateTime.UtcNow);

        Assert.Equal(RegistrationStatus.Cancelled, registration.Status);
        Assert.Equal(3, registration.Version);
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
