using EventOrganizer.Domain.Bookings;
using EventOrganizer.Domain.Events;
using EventOrganizer.Domain.Resources;
using EventOrganizer.Domain.Users;
using Microsoft.EntityFrameworkCore;

namespace EventOrganizer.Application.Common.Interfaces
{
    public interface IApplicationDbContext
    {
        DbSet<Event> Events { get; }

        DbSet<Resource> Resources { get; }

        DbSet<EventResourceBooking> EventResourceBookings { get; }

        DbSet<OrganizerRoleRequest> OrganizerRoleRequests { get; }

        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
