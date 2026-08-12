using EventOrganizer.Domain.Bookings;
using EventOrganizer.Domain.Events;
using EventOrganizer.Domain.Resources;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EventOrganizer.Infrastructure.Persistance.Configurations
{
    public sealed class EventResourceBookingConfiguration
        : IEntityTypeConfiguration<EventResourceBooking>
    {
        public void Configure(EntityTypeBuilder<EventResourceBooking> builder)
        {
            builder.ToTable("EventResourceBookings");

            builder.Property(booking => booking.Id)
                .ValueGeneratedNever();

            builder.Property(booking => booking.EventId)
                .IsRequired();

            builder.Property(booking => booking.Status)
                .HasConversion<string>()
                .HasMaxLength(50)
                .IsRequired();

            builder.Property(booking => booking.Version)
                .IsConcurrencyToken()
                .IsRequired();

            builder.Property(booking => booking.CreatedAtUtc)
                .IsRequired();

            builder.Property(booking => booking.SubmittedAtUtc);

            builder.Property(booking => booking.HoldExpiresAtUtc);

            builder.HasOne<Event>()
                .WithOne()
                .HasForeignKey<EventResourceBooking>(booking => booking.EventId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(booking => booking.Items)
                .WithOne()
                .HasForeignKey(item => item.BookingId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Navigation(booking => booking.Items)
                .UsePropertyAccessMode(PropertyAccessMode.Field);

            builder.HasIndex(booking => booking.EventId)
                .IsUnique();

            builder.HasIndex(booking => booking.Status);
        }
    }

    public sealed class EventResourceBookingItemConfiguration
        : IEntityTypeConfiguration<EventResourceBookingItem>
    {
        public void Configure(EntityTypeBuilder<EventResourceBookingItem> builder)
        {
            builder.ToTable("EventResourceBookingItems");

            builder.Property(item => item.Id)
                .ValueGeneratedNever();

            builder.Property(item => item.BookingId)
                .IsRequired();

            builder.Property(item => item.ResourceId)
                .IsRequired();

            builder.Property(item => item.ResourceType)
                .HasConversion<string>()
                .HasMaxLength(50)
                .IsRequired();

            builder.HasOne<Resource>()
                .WithMany()
                .HasForeignKey(item => item.ResourceId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(item => new { item.BookingId, item.ResourceId })
                .IsUnique();

            builder.HasIndex(item => item.ResourceId);
        }
    }
}
