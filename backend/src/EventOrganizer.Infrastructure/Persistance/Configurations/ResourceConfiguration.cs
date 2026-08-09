using EventOrganizer.Domain.Resources;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EventOrganizer.Infrastructure.Persistance.Configurations
{
    public sealed class ResourceConfiguration : IEntityTypeConfiguration<Resource>
    {
        public void Configure(EntityTypeBuilder<Resource> builder)
        {
            builder.HasDiscriminator(resource => resource.Type)
                .HasValue<Venue>(ResourceType.Venue)
                .HasValue<Speaker>(ResourceType.Speaker)
                .HasValue<EquipmentPackage>(ResourceType.EquipmentPackage);

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

            builder.Property(resource => resource.QualityScore)
                .IsRequired();

            builder.Property(resource => resource.Version)
                .IsConcurrencyToken()
                .IsRequired();

            builder.Property(resource => resource.CreatedAtUtc)
                .IsRequired();

            builder.HasIndex(resource => resource.Type);
            builder.HasIndex(resource => resource.Status);
        }
    }

    public sealed class VenueConfiguration : IEntityTypeConfiguration<Venue>
    {
        public void Configure(EntityTypeBuilder<Venue> builder)
        {
            builder.Property(venue => venue.Capacity)
                .HasColumnName("Capacity");
        }
    }

    public sealed class SpeakerConfiguration : IEntityTypeConfiguration<Speaker>
    {
        public void Configure(EntityTypeBuilder<Speaker> builder)
        {
            builder.Property(speaker => speaker.ExpertiseArea)
                .HasMaxLength(100);
        }
    }

    public sealed class EquipmentPackageConfiguration : IEntityTypeConfiguration<EquipmentPackage>
    {
        public void Configure(EntityTypeBuilder<EquipmentPackage> builder)
        {
            builder.Property(package => package.ProviderName)
                .HasMaxLength(200);

            builder.Property(package => package.ServiceArea)
                .HasMaxLength(100);

            builder.Property(package => package.ContentsSummary)
                .HasMaxLength(1000);

            builder.HasIndex(package => package.ServiceArea);
            builder.HasIndex(package => package.SupportedCapacity);
        }
    }
}
