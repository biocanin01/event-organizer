using EventOrganizer.Domain.Bookings;
using EventOrganizer.Domain.Events;
using EventOrganizer.Domain.Notifications;
using EventOrganizer.Domain.Resources;
using EventOrganizer.Domain.Registrations;
using EventOrganizer.Domain.Reviews;
using EventOrganizer.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using System.Data;

namespace EventOrganizer.Application.Common.Interfaces
{
    public interface IApplicationDbContext
    {
        DbSet<Event> Events { get; }

        DbSet<Notification> Notifications { get; }

        DbSet<Resource> Resources { get; }

        DbSet<EventResourceBooking> EventResourceBookings { get; }

        DbSet<Registration> Registrations { get; }

        DbSet<Review> Reviews { get; }

        DbSet<OrganizerRoleRequest> OrganizerRoleRequests { get; }

        Task<IDbContextTransaction> BeginTransactionAsync(
            IsolationLevel isolationLevel,
            CancellationToken cancellationToken = default);

        Task CommitTransactionAsync(
            IDbContextTransaction transaction,
            CancellationToken cancellationToken = default);

        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
