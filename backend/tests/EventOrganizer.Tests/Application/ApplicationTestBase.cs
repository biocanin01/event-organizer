using EventOrganizer.Domain.Events;
using EventOrganizer.Domain.Bookings;
using EventOrganizer.Domain.Resources;
using EventOrganizer.Application.Notifications;
using EventOrganizer.Infrastructure.Identity;
using EventOrganizer.Infrastructure.Persistance;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace EventOrganizer.Tests.Application
{
    public abstract class ApplicationTestBase : IDisposable
    {
        private readonly SqliteConnection _connection;

        protected ApplicationTestBase()
        {
            _connection = new SqliteConnection("DataSource=:memory:");
            _connection.Open();

            DbContext = CreateDbContext();
            DbContext.Database.EnsureCreated();
        }

        protected AppDbContext DbContext { get; }

        protected AppDbContext CreateDbContext()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlite(_connection)
                .Options;

            return new AppDbContext(options);
        }

        protected async Task<Guid> CreateOrganizerUserAsync(string? email = null)
        {
            var resolvedEmail = email ?? "organizer@example.com";

            var user = new ApplicationUser
            {
                Id = Guid.NewGuid(),
                UserName = resolvedEmail,
                Email = resolvedEmail,
                FullName = "Test Organizer",
                CreatedAtUtc = DateTime.UtcNow,
            };

            DbContext.Users.Add(user);
            await DbContext.SaveChangesAsync();

            return user.Id;
        }

        protected async Task<Event> CreateEventAsync(
            Guid? organizerUserId = null,
            string title = "Software Architecture Seminar",
            DateTime? startsAtUtc = null,
            DateTime? endsAtUtc = null,
            int capacity = 80,
            decimal budget = 1000m,
            string area = "IT",
            int requiredSpeakerCount = 1,
            bool requiresEquipment = false)
        {
            var resolvedOrganizerUserId = organizerUserId ?? await CreateOrganizerUserAsync();
            var resolvedStartsAtUtc = startsAtUtc ?? new DateTime(2026, 9, 1, 9, 0, 0, DateTimeKind.Utc);


            var eventItem = Event.Create(
                title,
                "Seminar about modern web architecture.",
                resolvedStartsAtUtc,
                endsAtUtc ?? resolvedStartsAtUtc.AddHours(4),
                capacity,
                budget,
                area,
                requiredSpeakerCount,
                resolvedOrganizerUserId,
                new DateTime(2026, 8, 1, 12, 0, 0, DateTimeKind.Utc),
                requiresEquipment);

            DbContext.Events.Add(eventItem);
            await DbContext.SaveChangesAsync();

            return eventItem;
        }

        protected async Task<EventResourceBooking> CreateBookingAsync(
            Event eventItem,
            params Resource[] resources)
        {
            var booking = EventResourceBooking.Create(
                eventItem.Id,
                DateTime.UtcNow);

            foreach (var resource in resources)
            {
                booking.AddResource(resource.Id, resource.Type, DateTime.UtcNow);
            }

            DbContext.EventResourceBookings.Add(booking);
            await DbContext.SaveChangesAsync();

            return booking;
        }

        protected async Task<EventResourceBooking> SetBookingStatusAsync(
            Guid bookingId,
            EventResourceBookingStatus status,
            DateTime? holdExpiresAtUtc = null)
        {
            var resolvedHoldExpiresAtUtc = holdExpiresAtUtc
                ?? (status == EventResourceBookingStatus.Submitted
                    ? DateTime.UtcNow.AddHours(1)
                    : null);

            await DbContext.Database.ExecuteSqlInterpolatedAsync(
                $"UPDATE EventResourceBookings SET Status = {status.ToString()}, HoldExpiresAtUtc = {resolvedHoldExpiresAtUtc} WHERE Id = {bookingId}");

            DbContext.ChangeTracker.Clear();

            return await DbContext.EventResourceBookings
                .Include(booking => booking.Items)
                .SingleAsync(booking => booking.Id == bookingId);
        }

        protected NotificationService CreateNotificationService()
        {
            return new NotificationService(DbContext);
        }

        public void Dispose()
        {
            DbContext.Dispose();
            _connection.Dispose();
        }
    }
}
