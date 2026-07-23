using EventOrganizer.Domain.Events;
using EventOrganizer.Domain.Resources;
using Microsoft.EntityFrameworkCore;

namespace EventOrganizer.Application.Common.Interfaces
{
    public interface IApplicationDbContext
    {
        DbSet<Event> Events { get; }

        DbSet<Resource> Resources { get; }

        DbSet<ResourceReservation> ResourceReservations { get; }

        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
