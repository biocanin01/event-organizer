using EventOrganizer.Domain.Events;
using EventOrganizer.Domain.Registrations;
using EventOrganizer.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EventOrganizer.Infrastructure.Persistance.Configurations
{
    public sealed class RegistrationConfiguration : IEntityTypeConfiguration<Registration>
    {
        public void Configure(EntityTypeBuilder<Registration> builder)
        {
            builder.Property(registration => registration.EventId)
                .IsRequired();

            builder.Property(registration => registration.ParticipantUserId)
                .IsRequired();

            builder.Property(registration => registration.Status)
                .HasConversion<string>()
                .HasMaxLength(50)
                .IsRequired();

            builder.Property(registration => registration.CreatedAtUtc)
                .IsRequired();

            builder.HasOne<Event>()
                .WithMany()
                .HasForeignKey(registration => registration.EventId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(registration => registration.ParticipantUserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(registration => registration.EventId);
            builder.HasIndex(registration => registration.ParticipantUserId);
            builder.HasIndex(registration => new
                {
                    registration.EventId,
                    registration.ParticipantUserId,
                })
                .IsUnique();
        }
    }
}
