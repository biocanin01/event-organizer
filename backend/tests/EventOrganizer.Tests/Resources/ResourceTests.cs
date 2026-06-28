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
            createdAtUtc);

        Assert.NotEqual(Guid.Empty, resource.Id);
        Assert.Equal(type, resource.Type);
        Assert.Equal(ResourceStatus.Available, resource.Status);
        Assert.Equal(createdAtUtc, resource.CreatedAtUtc);
    }

    [Fact]
    public void MarkUnavailable_WhenResourceIsAvailable_ChangesStatus()
    {
        var resource = Resource.Create(
            "Projector",
            "Conference projector.",
            ResourceType.Equipment,
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
            DateTime.UtcNow);

        resource.Archive(DateTime.UtcNow);

        var act = () => resource.UpdateDetails("New name", "New description.", DateTime.UtcNow);

        Assert.Throws<InvalidOperationException>(act);
    }
}
