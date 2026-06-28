using EventOrganizer.Domain.Events;
using EventOrganizer.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EventOrganizer.Infrastructure.Persistance.Configurations
{
    public sealed class EventConfiguration : IEntityTypeConfiguration<Event>
    {
        public void Configure(EntityTypeBuilder<Event> builder)
        {
            builder.Property(e => e.Title)
                .HasMaxLength(200)
                .IsRequired();

            builder.Property(e => e.Description)
                .HasMaxLength(2000)
                .IsRequired();

            builder.Property(e => e.Status)
                .HasConversion<string>()
                .HasMaxLength(50)
                .IsRequired();

            builder.Property(e => e.StartsAtUtc)
                .IsRequired();

            builder.Property(e => e.EndsAtUtc)
                .IsRequired();

            builder.Property(e => e.Capacity)
                .IsRequired();

            builder.Property(e => e.OrganizerUserId)
                .IsRequired();

            builder.Property(e => e.CreatedAtUtc)
                .IsRequired();

            builder.HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(e => e.OrganizerUserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(e => e.OrganizerUserId);
            builder.HasIndex(e => e.Status);
            builder.HasIndex(e => e.StartsAtUtc);
        }
    }
}
