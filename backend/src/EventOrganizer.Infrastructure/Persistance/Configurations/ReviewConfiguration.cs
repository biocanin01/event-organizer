using EventOrganizer.Domain.Events;
using EventOrganizer.Domain.Reviews;
using EventOrganizer.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EventOrganizer.Infrastructure.Persistance.Configurations
{
    public sealed class ReviewConfiguration : IEntityTypeConfiguration<Review>
    {
        public void Configure(EntityTypeBuilder<Review> builder)
        {
            builder.Property(review => review.EventId)
                .IsRequired();

            builder.Property(review => review.ParticipantUserId)
                .IsRequired();

            builder.Property(review => review.Rating)
                .IsRequired();

            builder.Property(review => review.Comment)
                .HasMaxLength(2000)
                .IsRequired();

            builder.Property(review => review.Version)
                .IsConcurrencyToken()
                .HasDefaultValue(1)
                .IsRequired();

            builder.Property(review => review.CreatedAtUtc)
                .IsRequired();

            builder.HasOne<Event>()
                .WithMany()
                .HasForeignKey(review => review.EventId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(review => review.ParticipantUserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(review => review.EventId);
            builder.HasIndex(review => review.ParticipantUserId);
            builder.HasIndex(review => new
            {
                review.EventId,
                review.ParticipantUserId,
            })
                .IsUnique();
        }
    }
}
