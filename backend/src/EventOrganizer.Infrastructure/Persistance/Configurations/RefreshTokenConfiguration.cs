using EventOrganizer.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EventOrganizer.Infrastructure.Persistance.Configurations
{
    public sealed class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
    {
        public void Configure(EntityTypeBuilder<RefreshToken> builder)
        {
            builder.Property(refreshToken => refreshToken.UserId)
                .IsRequired();

            builder.Property(refreshToken => refreshToken.TokenHash)
                .HasMaxLength(512)
                .IsRequired();

            builder.Property(refreshToken => refreshToken.ExpiresAtUtc)
                .IsRequired();

            builder.Property(refreshToken => refreshToken.CreatedAtUtc)
                .IsRequired();

            builder.Property(refreshToken => refreshToken.ReplacedByTokenHash)
                .HasMaxLength(512);

            builder.Property(refreshToken => refreshToken.CreatedByIpAddress)
                .HasMaxLength(100);

            builder.Property(refreshToken => refreshToken.RevokedByIpAddress)
                .HasMaxLength(100);

            builder.HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(refreshToken => refreshToken.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(refreshToken => refreshToken.TokenHash)
                .IsUnique();

            builder.HasIndex(refreshToken => refreshToken.UserId);
            builder.HasIndex(refreshToken => refreshToken.ExpiresAtUtc);
        }
    }
}
