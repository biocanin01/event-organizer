using EventOrganizer.Domain.Resources;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EventOrganizer.Infrastructure.Persistance.Configurations
{
    public sealed class ResourceConfiguration : IEntityTypeConfiguration<Resource>
    {
        public void Configure(EntityTypeBuilder<Resource> builder)
        {
            builder.Property(resource => resource.Name)
                .HasMaxLength(200)
                .IsRequired();

            builder.Property(resource => resource.Description)
                .HasMaxLength(2000)
                .IsRequired();

            builder.Property(resource => resource.Type)
                .HasConversion<string>()
                .HasMaxLength(50)
                .IsRequired();

            builder.Property(resource => resource.Status)
                .HasConversion<string>()
                .HasMaxLength(50)
                .IsRequired();

            builder.Property(resource => resource.Cost)
                .HasPrecision(18, 2)
                .IsRequired();

            builder.Property(resource => resource.Capacity);

            builder.Property(resource => resource.Area)
                .HasMaxLength(100);

            builder.Property(resource => resource.QualityScore)
                .IsRequired();

            builder.Property(resource => resource.CreatedAtUtc)
                .IsRequired();

            builder.HasIndex(resource => resource.Type);
            builder.HasIndex(resource => resource.Status);
            builder.HasIndex(resource => resource.Area);
        }
    }
}
