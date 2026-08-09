using EventOrganizer.Domain.Bookings;
using EventOrganizer.Domain.Resources;
using Microsoft.EntityFrameworkCore;

namespace EventOrganizer.Tests.Application.Bookings
{
    public sealed class EventResourceBookingPersistenceTests : ApplicationTestBase
    {
        [Fact]
        public async Task SaveChanges_PersistsBookingWithItems()
        {
            var eventItem = await CreateEventAsync();
            var venue = TestResourceFactory.Create(
                "Main Hall",
                "Conference venue.",
                ResourceType.Venue,
                500m,
                120,
                null,
                4,
                DateTime.UtcNow);
            var speaker = TestResourceFactory.Create(
                "Architecture Speaker",
                "Speaker profile.",
                ResourceType.Speaker,
                200m,
                null,
                "IT",
                5,
                DateTime.UtcNow);
            DbContext.Resources.AddRange(venue, speaker);
            await DbContext.SaveChangesAsync();

            var booking = await CreateBookingAsync(eventItem, venue, speaker);
            DbContext.ChangeTracker.Clear();

            var persistedBooking = await DbContext.EventResourceBookings
                .Include(item => item.Items)
                .SingleAsync(item => item.Id == booking.Id);

            Assert.Equal(eventItem.Id, persistedBooking.EventId);
            Assert.Equal(2, persistedBooking.Items.Count);
            Assert.Contains(
                persistedBooking.Items,
                item => item.ResourceId == venue.Id && item.ResourceType == ResourceType.Venue);
            Assert.Contains(
                persistedBooking.Items,
                item => item.ResourceId == speaker.Id && item.ResourceType == ResourceType.Speaker);
        }

        [Fact]
        public async Task SaveChanges_WhenEventAlreadyHasBooking_Throws()
        {
            var eventItem = await CreateEventAsync();
            await CreateBookingAsync(eventItem);
            DbContext.ChangeTracker.Clear();
            DbContext.EventResourceBookings.Add(
                EventResourceBooking.Create(eventItem.Id, DateTime.UtcNow));

            await Assert.ThrowsAsync<DbUpdateException>(() => DbContext.SaveChangesAsync());
        }

        [Fact]
        public void Model_HasExpectedUniqueBookingIndexes()
        {
            var bookingType = DbContext.Model.FindEntityType(typeof(EventResourceBooking));
            var itemType = DbContext.Model.FindEntityType(typeof(EventResourceBookingItem));

            Assert.NotNull(bookingType);
            Assert.NotNull(itemType);
            Assert.Contains(
                bookingType.GetIndexes(),
                index => index.IsUnique
                    && index.Properties.Select(property => property.Name)
                        .SequenceEqual(new[] { nameof(EventResourceBooking.EventId) }));
            Assert.Contains(
                itemType.GetIndexes(),
                index => index.IsUnique
                    && index.Properties.Select(property => property.Name)
                        .SequenceEqual(new[]
                        {
                            nameof(EventResourceBookingItem.BookingId),
                            nameof(EventResourceBookingItem.ResourceId),
                        }));
        }
    }
}
