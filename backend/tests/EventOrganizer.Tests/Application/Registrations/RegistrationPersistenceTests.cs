using EventOrganizer.Domain.Registrations;
using Microsoft.EntityFrameworkCore;

namespace EventOrganizer.Tests.Application.Registrations
{
    public sealed class RegistrationPersistenceTests : ApplicationTestBase
    {
        [Fact]
        public async Task SaveChanges_WithStaleRegistrationVersion_ThrowsConcurrencyException()
        {
            var organizerUserId = await CreateOrganizerUserAsync();
            var participantUserId = await CreateOrganizerUserAsync("registration-participant@example.com");
            var eventItem = await CreateEventAsync(organizerUserId);
            var registration = Registration.Create(eventItem.Id, participantUserId, DateTime.UtcNow);
            DbContext.Registrations.Add(registration);
            await DbContext.SaveChangesAsync();
            DbContext.ChangeTracker.Clear();

            using var secondContext = CreateDbContext();
            var firstCopy = await DbContext.Registrations.SingleAsync(item => item.Id == registration.Id);
            var secondCopy = await secondContext.Registrations.SingleAsync(item => item.Id == registration.Id);
            firstCopy.Confirm(organizerUserId, DateTime.UtcNow);
            secondCopy.Reject("Capacity reached.", organizerUserId, DateTime.UtcNow);

            await DbContext.SaveChangesAsync();

            await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() =>
                secondContext.SaveChangesAsync());
        }
    }
}
