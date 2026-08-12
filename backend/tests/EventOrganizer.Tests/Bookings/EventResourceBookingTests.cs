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

        [Fact]
        public void Submit_WhenBookingIsDraft_SubmitsWithHold()
        {
            var booking = CreateBooking();
            var submittedAtUtc = DateTime.UtcNow;
            var holdExpiresAtUtc = submittedAtUtc.AddHours(48);

            booking.Submit(submittedAtUtc, holdExpiresAtUtc);

            Assert.Equal(EventResourceBookingStatus.Submitted, booking.Status);
            Assert.Equal(submittedAtUtc, booking.SubmittedAtUtc);
            Assert.Equal(holdExpiresAtUtc, booking.HoldExpiresAtUtc);
            Assert.Equal(2, booking.Version);
        }

        [Fact]
        public void Withdraw_WhenBookingIsSubmitted_ReturnsToDraftAndClearsHold()
        {
            var booking = CreateBooking();
            booking.Submit(DateTime.UtcNow, DateTime.UtcNow.AddHours(48));

            booking.Withdraw(DateTime.UtcNow);

            Assert.Equal(EventResourceBookingStatus.Draft, booking.Status);
            Assert.Null(booking.SubmittedAtUtc);
            Assert.Null(booking.HoldExpiresAtUtc);
            Assert.Equal(3, booking.Version);
        }

        [Fact]
        public void Approve_WhenSubmittedHoldIsActive_ApprovesAndStoresDecision()
        {
            var booking = CreateBooking();
            var submittedAtUtc = DateTime.UtcNow;
            var adminUserId = Guid.NewGuid();
            booking.Submit(submittedAtUtc, submittedAtUtc.AddHours(48));

            booking.Approve(adminUserId, submittedAtUtc.AddHours(1));

            Assert.Equal(EventResourceBookingStatus.Approved, booking.Status);
            Assert.Equal(adminUserId, booking.DecidedByUserId);
            Assert.Equal(submittedAtUtc.AddHours(1), booking.DecidedAtUtc);
            Assert.Null(booking.DecisionReason);
            Assert.Equal(3, booking.Version);
        }

        [Fact]
        public void Approve_WhenHoldExpired_DoesNotChangeBooking()
        {
            var booking = CreateBooking();
            var submittedAtUtc = DateTime.UtcNow;
            booking.Submit(submittedAtUtc, submittedAtUtc.AddHours(48));
            var version = booking.Version;

            var action = () => booking.Approve(Guid.NewGuid(), submittedAtUtc.AddHours(49));

            Assert.Throws<InvalidOperationException>(action);
            Assert.Equal(EventResourceBookingStatus.Submitted, booking.Status);
            Assert.Equal(version, booking.Version);
        }

        [Fact]
        public void Reject_WhenSubmitted_RejectsAndStoresTrimmedDecision()
        {
            var booking = CreateBooking();
            var submittedAtUtc = DateTime.UtcNow;
            var adminUserId = Guid.NewGuid();
            booking.Submit(submittedAtUtc, submittedAtUtc.AddHours(48));

            booking.Reject(adminUserId, "  Missing contract.  ", submittedAtUtc.AddHours(1));

            Assert.Equal(EventResourceBookingStatus.Rejected, booking.Status);
            Assert.Equal("Missing contract.", booking.DecisionReason);
            Assert.Equal(adminUserId, booking.DecidedByUserId);
            Assert.Equal(3, booking.Version);
        }

        [Fact]
        public void Expire_WhenSubmittedHoldExpired_ExpiresAndIncrementsVersion()
        {
            var booking = CreateBooking();
            var submittedAtUtc = DateTime.UtcNow;
            booking.Submit(submittedAtUtc, submittedAtUtc.AddHours(48));

            var expired = booking.Expire(submittedAtUtc.AddHours(49));

            Assert.True(expired);
            Assert.Equal(EventResourceBookingStatus.Expired, booking.Status);
            Assert.Equal(3, booking.Version);
        }

        [Fact]
        public void Expire_WhenHoldIsActive_DoesNotChangeBooking()
        {
            var booking = CreateBooking();
            var submittedAtUtc = DateTime.UtcNow;
            booking.Submit(submittedAtUtc, submittedAtUtc.AddHours(48));
            var version = booking.Version;

            var expired = booking.Expire(submittedAtUtc.AddHours(1));

            Assert.False(expired);
            Assert.Equal(EventResourceBookingStatus.Submitted, booking.Status);
            Assert.Equal(version, booking.Version);
        }

        private static EventResourceBooking CreateBooking()
        {
            return EventResourceBooking.Create(Guid.NewGuid(), DateTime.UtcNow);
        }
    }
}
