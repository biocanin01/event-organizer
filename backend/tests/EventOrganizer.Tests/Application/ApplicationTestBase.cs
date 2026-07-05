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

        public void Dispose()
        {
            DbContext.Dispose();
            _connection.Dispose();
        }
    }
}
