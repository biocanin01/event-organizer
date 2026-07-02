using EventOrganizer.Application.Common.Interfaces;
using EventOrganizer.Domain.Events;
using EventOrganizer.Domain.Registrations;
using EventOrganizer.Domain.Resources;
using EventOrganizer.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace EventOrganizer.Infrastructure.Persistance
{
    public sealed class AppDbContext : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>, IApplicationDbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Event> Events => Set<Event>();

        public DbSet<Resource> Resources => Set<Resource>();

        public DbSet<ResourceReservation> ResourceReservations => Set<ResourceReservation>();

        public DbSet<Registration> Registrations => Set<Registration>();

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        }
    }
}
