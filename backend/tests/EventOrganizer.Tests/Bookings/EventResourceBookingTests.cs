using EventOrganizer.Domain.Bookings;
using EventOrganizer.Domain.Resources;

namespace EventOrganizer.Tests.Bookings
{
    public sealed class EventResourceBookingTests
    {
        [Fact]
        public void Create_WithValidEventId_CreatesEmptyDraft()
        {
            var createdAtUtc = DateTime.UtcNow;

            var booking = EventResourceBooking.Create(Guid.NewGuid(), createdAtUtc);

            Assert.NotEqual(Guid.Empty, booking.Id);
            Assert.Equal(EventResourceBookingStatus.Draft, booking.Status);
            Assert.Equal(1, booking.Version);
            Assert.Empty(booking.Items);
            Assert.Equal(createdAtUtc, booking.CreatedAtUtc);
        }

        [Fact]
        public void Create_WithoutEventId_Throws()
        {
            var action = () => EventResourceBooking.Create(Guid.Empty, DateTime.UtcNow);

            Assert.Throws<ArgumentException>(action);
        }

        [Fact]
        public void AddResource_AllowsOneVenueMultipleSpeakersAndOnePackage()
        {
            var booking = CreateBooking();

            booking.AddResource(Guid.NewGuid(), ResourceType.Venue, DateTime.UtcNow);
            booking.AddResource(Guid.NewGuid(), ResourceType.Speaker, DateTime.UtcNow);
            booking.AddResource(Guid.NewGuid(), ResourceType.Speaker, DateTime.UtcNow);
            booking.AddResource(Guid.NewGuid(), ResourceType.EquipmentPackage, DateTime.UtcNow);

            Assert.Equal(4, booking.Items.Count);
            Assert.Equal(5, booking.Version);
        }

        [Fact]
        public void AddResource_WhenResourceAlreadyExists_DoesNotChangeBooking()
        {
            var booking = CreateBooking();
            var resourceId = Guid.NewGuid();
            booking.AddResource(resourceId, ResourceType.Speaker, DateTime.UtcNow);
            var version = booking.Version;

            var action = () => booking.AddResource(
                resourceId,
                ResourceType.Speaker,
                DateTime.UtcNow);

            Assert.Throws<InvalidOperationException>(action);
            Assert.Single(booking.Items);
            Assert.Equal(version, booking.Version);
        }

        [Theory]
        [InlineData(ResourceType.Venue)]
        [InlineData(ResourceType.EquipmentPackage)]
        public void AddResource_WhenSingleSelectionTypeAlreadyExists_DoesNotChangeBooking(
            ResourceType resourceType)
        {
            var booking = CreateBooking();
            booking.AddResource(Guid.NewGuid(), resourceType, DateTime.UtcNow);
            var version = booking.Version;

            var action = () => booking.AddResource(
                Guid.NewGuid(),
                resourceType,
                DateTime.UtcNow);

            Assert.Throws<InvalidOperationException>(action);
            Assert.Single(booking.Items);
            Assert.Equal(version, booking.Version);
        }

        [Fact]
        public void RemoveResource_WhenResourceExists_RemovesItemAndIncrementsVersion()
        {
            var booking = CreateBooking();
            var resourceId = Guid.NewGuid();
            booking.AddResource(resourceId, ResourceType.Speaker, DateTime.UtcNow);

            booking.RemoveResource(resourceId, DateTime.UtcNow);

            Assert.Empty(booking.Items);
            Assert.Equal(3, booking.Version);
        }

        [Fact]
        public void RemoveResource_WhenResourceDoesNotExist_DoesNotChangeBooking()
        {
            var booking = CreateBooking();

            var action = () => booking.RemoveResource(Guid.NewGuid(), DateTime.UtcNow);

            Assert.Throws<InvalidOperationException>(action);
            Assert.Empty(booking.Items);
            Assert.Equal(1, booking.Version);
        }

        [Fact]
        public void RemoveResource_WithoutResourceId_DoesNotChangeBooking()
        {
            var booking = CreateBooking();

            var action = () => booking.RemoveResource(Guid.Empty, DateTime.UtcNow);

            Assert.Throws<ArgumentException>(action);
            Assert.Empty(booking.Items);
            Assert.Equal(1, booking.Version);
        }

        [Fact]
        public void Cancel_WhenBookingIsDraft_CancelsBookingAndPreventsEditing()
        {
            var booking = CreateBooking();

            booking.Cancel(DateTime.UtcNow);

            Assert.Equal(EventResourceBookingStatus.Cancelled, booking.Status);
            Assert.Equal(2, booking.Version);
            Assert.Throws<InvalidOperationException>(() => booking.AddResource(
                Guid.NewGuid(),
                ResourceType.Speaker,
                DateTime.UtcNow));
        }

        private static EventResourceBooking CreateBooking()
        {
            return EventResourceBooking.Create(Guid.NewGuid(), DateTime.UtcNow);
        }
    }
}
