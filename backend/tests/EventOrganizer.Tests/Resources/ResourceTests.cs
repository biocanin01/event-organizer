using EventOrganizer.Domain.Resources;

namespace EventOrganizer.Tests.Resources;

public sealed class ResourceTests
{
    [Theory]
    [InlineData(ResourceType.Venue)]
    [InlineData(ResourceType.Speaker)]
    [InlineData(ResourceType.Equipment)]
    [InlineData(ResourceType.TechnicalSupport)]
    public void Create_WithSupportedType_CreatesAvailableResource(ResourceType type)
    {
        var createdAtUtc = new DateTime(2026, 8, 1, 12, 0, 0, DateTimeKind.Utc);

        var resource = Resource.Create(
            "Main hall",
            "Primary conference hall.",
            type,
            500m,
            100,
            "IT",
            4,
            createdAtUtc);

        Assert.NotEqual(Guid.Empty, resource.Id);
        Assert.Equal(type, resource.Type);
        Assert.Equal(500m, resource.Cost);
        Assert.Equal(100, resource.Capacity);
        Assert.Equal("IT", resource.Area);
        Assert.Equal(4, resource.QualityScore);
        Assert.Equal(ResourceStatus.Available, resource.Status);
        Assert.Equal(createdAtUtc, resource.CreatedAtUtc);
    }

    [Fact]
    public void Create_WhenCostIsNegative_Throws()
    {
        var act = () => Resource.Create(
            "Projector",
            "Conference projector.",
            ResourceType.Equipment,
            -1m,
            null,
            null,
            3,
            DateTime.UtcNow);

        Assert.Throws<ArgumentOutOfRangeException>(act);
    }

    [Fact]
    public void Create_WhenCapacityIsNotPositive_Throws()
    {
        var act = () => Resource.Create(
            "Main hall",
            "Primary conference hall.",
            ResourceType.Venue,
            500m,
            0,
            "IT",
            3,
            DateTime.UtcNow);

        Assert.Throws<ArgumentOutOfRangeException>(act);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(6)]
    public void Create_WhenQualityScoreIsOutsideRange_Throws(int qualityScore)
    {
        var act = () => Resource.Create(
            "Main hall",
            "Primary conference hall.",
            ResourceType.Venue,
            500m,
            100,
            "IT",
            qualityScore,
            DateTime.UtcNow);

        Assert.Throws<ArgumentOutOfRangeException>(act);
    }

    [Fact]
    public void MarkUnavailable_WhenResourceIsAvailable_ChangesStatus()
    {
        var resource = Resource.Create(
            "Projector",
            "Conference projector.",
            ResourceType.Equipment,
            100m,
            null,
            null,
            3,
            DateTime.UtcNow);

        resource.MarkUnavailable(DateTime.UtcNow);

        Assert.Equal(ResourceStatus.Unavailable, resource.Status);
    }

    [Fact]
    public void UpdateDetails_WhenResourceIsArchived_Throws()
    {
        var resource = Resource.Create(
            "Guest speaker",
            "Speaker profile.",
            ResourceType.Speaker,
            300m,
            null,
            "IT",
            4,
            DateTime.UtcNow);

        resource.Archive(DateTime.UtcNow);

        var act = () => resource.UpdateDetails(
            "New name",
            "New description.",
            350m,
            null,
            "IT",
            4,
            DateTime.UtcNow);

        Assert.Throws<InvalidOperationException>(act);
    }
}
