using EventOrganizer.Domain.Users;
using EventOrganizer.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EventOrganizer.Infrastructure.Persistance.Configurations
{
    public sealed class OrganizerRoleRequestConfiguration
        : IEntityTypeConfiguration<OrganizerRoleRequest>
    {
        public void Configure(EntityTypeBuilder<OrganizerRoleRequest> builder)
        {
            builder.Property(request => request.UserId)
                .IsRequired();

            builder.Property(request => request.Motivation)
                .HasMaxLength(OrganizerRoleRequest.MaxMotivationLength)
                .IsRequired();

            builder.Property(request => request.Status)
                .HasConversion<string>()
                .HasMaxLength(50)
                .IsRequired();

            builder.Property(request => request.DecisionReason)
                .HasMaxLength(OrganizerRoleRequest.MaxDecisionReasonLength);

            builder.Property(request => request.SubmittedAtUtc)
                .IsRequired();

            builder.Property(request => request.Version)
                .IsConcurrencyToken()
                .IsRequired();

            builder.HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(request => request.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(request => request.ReviewedByAdminUserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(request => request.UserId);
            builder.HasIndex(request => request.Status);
            builder.HasIndex(request => request.SubmittedAtUtc);
            builder.HasIndex(request => request.UserId)
                .HasFilter("\"Status\" = 'Pending'")
                .IsUnique();
        }
    }
}
