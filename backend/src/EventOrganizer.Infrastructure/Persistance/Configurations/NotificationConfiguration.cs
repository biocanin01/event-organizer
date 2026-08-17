using EventOrganizer.Domain.Notifications;
using EventOrganizer.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EventOrganizer.Infrastructure.Persistance.Configurations
{
    public sealed class NotificationConfiguration : IEntityTypeConfiguration<Notification>
    {
        public void Configure(EntityTypeBuilder<Notification> builder)
        {
            builder.Property(notification => notification.RecipientUserId)
                .IsRequired();

            builder.Property(notification => notification.Type)
                .HasConversion<string>()
                .HasMaxLength(50)
                .IsRequired();

            builder.Property(notification => notification.Title)
                .HasMaxLength(Notification.MaxTitleLength)
                .IsRequired();

            builder.Property(notification => notification.Message)
                .HasMaxLength(Notification.MaxMessageLength)
                .IsRequired();

            builder.Property(notification => notification.RelatedEntityType)
                .HasConversion<string>()
                .HasMaxLength(50);

            builder.Property(notification => notification.CreatedAtUtc)
                .IsRequired();

            builder.Property(notification => notification.Version)
                .IsConcurrencyToken()
                .HasDefaultValue(1)
                .IsRequired();

            builder.Ignore(notification => notification.IsRead);

            builder.HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(notification => notification.RecipientUserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(notification => new
            {
                notification.RecipientUserId,
                notification.CreatedAtUtc,
            });

            builder.HasIndex(notification => notification.RecipientUserId)
                .HasDatabaseName("IX_Notifications_RecipientUserId_Unread")
                .HasFilter("\"ReadAtUtc\" IS NULL");
        }
    }
}
