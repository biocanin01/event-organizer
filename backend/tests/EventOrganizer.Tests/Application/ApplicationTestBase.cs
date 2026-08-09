using EventOrganizer.Domain.Events;
using EventOrganizer.Domain.Bookings;
using EventOrganizer.Domain.Resources;
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

            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlite(_connection)
                .Options;

            DbContext = new AppDbContext(options);
            DbContext.Database.EnsureCreated();
        }

        protected AppDbContext DbContext { get; }

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
            DateTime? startsAtUtc = null)
        {
            var resolvedOrganizerUserId = organizerUserId ?? await CreateOrganizerUserAsync();
            var resolvedStartsAtUtc = startsAtUtc ?? new DateTime(2026, 9, 1, 9, 0, 0, DateTimeKind.Utc);


            var eventItem = Event.Create(
                title,
                "Seminar about modern web architecture.",
                resolvedStartsAtUtc,
                resolvedStartsAtUtc.AddHours(4),
                80,
                1000m,
                "IT",
                1,
                resolvedOrganizerUserId,
                new DateTime(2026, 8, 1, 12, 0, 0, DateTimeKind.Utc));

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
            EventResourceBookingStatus status)
        {
            await DbContext.Database.ExecuteSqlInterpolatedAsync(
                $"UPDATE EventResourceBookings SET Status = {status.ToString()} WHERE Id = {bookingId}");

            DbContext.ChangeTracker.Clear();

            return await DbContext.EventResourceBookings
                .Include(booking => booking.Items)
                .SingleAsync(booking => booking.Id == bookingId);
        }

        public void Dispose()
        {
            DbContext.Dispose();
            _connection.Dispose();
        }
    }
}
