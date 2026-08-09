using EventOrganizer.Domain.Resources;

namespace EventOrganizer.Tests.Resources;

public sealed class ResourceTests
{
    [Fact]
    public void CreateVenue_WithValidDetails_CreatesAvailableVenue()
    {
        var createdAtUtc = new DateTime(2026, 8, 1, 12, 0, 0, DateTimeKind.Utc);

        var resource = Venue.Create(
            "Main hall",
            "Primary conference hall.",
            500m,
            100,
            4,
            createdAtUtc);

        Assert.NotEqual(Guid.Empty, resource.Id);
        Assert.Equal(ResourceType.Venue, resource.Type);
        Assert.Equal(500m, resource.Cost);
        Assert.Equal(100, resource.Capacity);
        Assert.Equal(4, resource.QualityScore);
        Assert.Equal(1, resource.Version);
        Assert.Equal(ResourceStatus.Available, resource.Status);
        Assert.Equal(createdAtUtc, resource.CreatedAtUtc);
    }

    [Fact]
    public void CreateSpeaker_WithValidDetails_CreatesAvailableSpeaker()
    {
        var resource = Speaker.Create(
            "Guest speaker",
            "Speaker profile.",
            300m,
            "IT",
            4,
            DateTime.UtcNow);

        Assert.Equal(ResourceType.Speaker, resource.Type);
        Assert.Equal("IT", resource.ExpertiseArea);
        Assert.Equal(ResourceStatus.Available, resource.Status);
    }

    [Fact]
    public void CreateEquipmentPackage_WithValidDetails_CreatesAvailablePackage()
    {
        var resource = EquipmentPackage.Create(
            "Conference AV package",
            "Audio and video package.",
            250m,
            "AV Supplier",
            150,
            "Belgrade",
            true,
            "Projector, microphones and on-site setup.",
            5,
            DateTime.UtcNow);

        Assert.Equal(ResourceType.EquipmentPackage, resource.Type);
        Assert.Equal("AV Supplier", resource.ProviderName);
        Assert.Equal(150, resource.SupportedCapacity);
        Assert.Equal("Belgrade", resource.ServiceArea);
        Assert.True(resource.IncludesTechnicalSupport);
        Assert.Equal("Projector, microphones and on-site setup.", resource.ContentsSummary);
    }

    [Fact]
    public void CreateVenue_WhenCostIsNegative_Throws()
    {
        var act = () => Venue.Create(
            "Main hall",
            "Primary conference hall.",
            -1m,
            100,
            3,
            DateTime.UtcNow);

        Assert.Throws<ArgumentOutOfRangeException>(act);
    }

    [Fact]
    public void CreateVenue_WhenCapacityIsNotPositive_Throws()
    {
        var act = () => Venue.Create(
            "Main hall",
            "Primary conference hall.",
            500m,
            0,
            3,
            DateTime.UtcNow);

        Assert.Throws<ArgumentOutOfRangeException>(act);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(6)]
    public void CreateVenue_WhenQualityScoreIsOutsideRange_Throws(int qualityScore)
    {
        var act = () => Venue.Create(
            "Main hall",
            "Primary conference hall.",
            500m,
            100,
            qualityScore,
            DateTime.UtcNow);

        Assert.Throws<ArgumentOutOfRangeException>(act);
    }

    [Fact]
    public void MarkUnavailable_WhenResourceIsAvailable_ChangesStatusAndIncrementsVersion()
    {
        var resource = EquipmentPackage.Create(
            "Conference AV package",
            "Audio and video package.",
            250m,
            "AV Supplier",
            150,
            "Belgrade",
            true,
            "Projector, microphones and on-site setup.",
            5,
            DateTime.UtcNow);

        resource.MarkUnavailable(DateTime.UtcNow);

        Assert.Equal(ResourceStatus.Unavailable, resource.Status);
        Assert.Equal(2, resource.Version);
    }

    [Fact]
    public void UpdateDetails_WhenResourceIsArchived_Throws()
    {
        var resource = Speaker.Create(
            "Guest speaker",
            "Speaker profile.",
            300m,
            "IT",
            4,
            DateTime.UtcNow);

        resource.Archive(DateTime.UtcNow);

        var act = () => resource.UpdateDetails(
            "New name",
            "New description.",
            350m,
            "IT",
            4,
            DateTime.UtcNow);

        Assert.Throws<InvalidOperationException>(act);
    }

    [Fact]
    public void UpdateVenue_WhenCapacityIsInvalid_DoesNotPartiallyChangeResource()
    {
        var resource = Venue.Create(
            "Main hall",
            "Primary conference hall.",
            500m,
            100,
            4,
            DateTime.UtcNow);

        var act = () => resource.UpdateDetails(
            "Changed name",
            "Changed description.",
            600m,
            0,
            5,
            DateTime.UtcNow);

        Assert.Throws<ArgumentOutOfRangeException>(act);
        Assert.Equal("Main hall", resource.Name);
        Assert.Equal(500m, resource.Cost);
        Assert.Equal(100, resource.Capacity);
        Assert.Equal(1, resource.Version);
    }
}
