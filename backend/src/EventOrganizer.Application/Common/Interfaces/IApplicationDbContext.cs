using EventOrganizer.Domain.Events;
using Microsoft.EntityFrameworkCore;

namespace EventOrganizer.Application.Common.Interfaces
{
    public interface IApplicationDbContext
    {
        DbSet<Event> Events { get; }

        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
