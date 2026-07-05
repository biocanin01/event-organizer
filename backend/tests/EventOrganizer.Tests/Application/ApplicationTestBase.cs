using EventOrganizer.Domain.Events;
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

        protected async Task<Guid> CreateOrganizerUserAsync()
        {
            var user = new ApplicationUser
            {
                Id = Guid.NewGuid(),
                UserName = "organizer@example.com",
                Email = "organizer@example.com",
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
                resolvedOrganizerUserId,
                new DateTime(2026, 8, 1, 12, 0, 0, DateTimeKind.Utc));

            DbContext.Events.Add(eventItem);
            await DbContext.SaveChangesAsync();

            return eventItem;
        }

        public void Dispose()
        {
            DbContext.Dispose();
            _connection.Dispose();
        }
    }
}
