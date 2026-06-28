using EventOrganizer.Domain.Events;
using EventOrganizer.Domain.Resources;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EventOrganizer.Infrastructure.Persistance.Configurations
{
    public sealed class ResourceReservationConfiguration : IEntityTypeConfiguration<ResourceReservation>
    {
        public void Configure(EntityTypeBuilder<ResourceReservation> builder)
        {
            builder.Property(reservation => reservation.EventId)
                .IsRequired();

            builder.Property(reservation => reservation.ResourceId)
                .IsRequired();

            builder.Property(reservation => reservation.StartsAtUtc)
                .IsRequired();

            builder.Property(reservation => reservation.EndsAtUtc)
                .IsRequired();

            builder.Property(reservation => reservation.Status)
                .HasConversion<string>()
                .HasMaxLength(50)
                .IsRequired();

            builder.Property(reservation => reservation.CreatedAtUtc)
                .IsRequired();

            builder.HasOne<Event>()
                .WithMany()
                .HasForeignKey(reservation => reservation.EventId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne<Resource>()
                .WithMany()
                .HasForeignKey(reservation => reservation.ResourceId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(reservation => reservation.EventId);
            builder.HasIndex(reservation => reservation.ResourceId);
            builder.HasIndex(reservation => reservation.Status);
        }
    }
}
