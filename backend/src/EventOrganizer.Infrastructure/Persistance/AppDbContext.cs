using EventOrganizer.Application.Common.Interfaces;
using EventOrganizer.Domain.Bookings;
using EventOrganizer.Domain.Events;
using EventOrganizer.Domain.Notifications;
using EventOrganizer.Domain.Registrations;
using EventOrganizer.Domain.Resources;
using EventOrganizer.Domain.Reviews;
using EventOrganizer.Domain.Users;
using EventOrganizer.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Npgsql;
using System.Data;

namespace EventOrganizer.Infrastructure.Persistance
{
    public sealed class AppDbContext : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>, IApplicationDbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Event> Events => Set<Event>();

        public DbSet<Notification> Notifications => Set<Notification>();

        public DbSet<Resource> Resources => Set<Resource>();

        public DbSet<EventResourceBooking> EventResourceBookings => Set<EventResourceBooking>();

        public DbSet<Registration> Registrations => Set<Registration>();

        public DbSet<Review> Reviews => Set<Review>();

        public DbSet<OrganizerRoleRequest> OrganizerRoleRequests => Set<OrganizerRoleRequest>();

        public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

        public Task<IDbContextTransaction> BeginTransactionAsync(
            IsolationLevel isolationLevel,
            CancellationToken cancellationToken = default)
        {
            return Database.BeginTransactionAsync(isolationLevel, cancellationToken);
        }

        public async Task CommitTransactionAsync(
            IDbContextTransaction transaction,
            CancellationToken cancellationToken = default)
        {
            try
            {
                await transaction.CommitAsync(cancellationToken);
            }
            catch (PostgresException exception) when (
                exception.SqlState is PostgresErrorCodes.SerializationFailure
                    or PostgresErrorCodes.DeadlockDetected)
            {
                throw new DbUpdateConcurrencyException(
                    "The transaction could not be committed because of a concurrent database change.",
                    exception);
            }
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        }
    }
}
