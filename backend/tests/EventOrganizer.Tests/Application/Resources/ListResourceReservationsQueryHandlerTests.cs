using EventOrganizer.Application.Queries.ListResourceReservations;
using EventOrganizer.Domain.Resources;

namespace EventOrganizer.Tests.Application.Resources
{
    public sealed class ListResourceReservationsQueryHandlerTests : ApplicationTestBase
    {
        [Fact]
        public async Task Handle_ReturnsReservationsOrderedByStartDate()
        {
            var laterReservation = await CreateReservationAsync(
                new DateTime(2026, 9, 2, 9, 0, 0, DateTimeKind.Utc));
            var earlierReservation = await CreateReservationAsync(
                new DateTime(2026, 9, 1, 9, 0, 0, DateTimeKind.Utc));
            var handler = new ListResourceReservationsQueryHandler(DbContext);

            var result = await handler.Handle(
                new ListResourceReservationsQuery(),
                CancellationToken.None);

            Assert.Equal(2, result.Count);
            Assert.Equal(earlierReservation.Id, result[0].Id);
            Assert.Equal(laterReservation.Id, result[1].Id);
        }

        private async Task<ResourceReservation> CreateReservationAsync(
            DateTime startsAtUtc)
        {
            var eventItem = await CreateEventAsync(startsAtUtc: startsAtUtc);
            var resource = Resource.Create(
                $"Conference Hall {startsAtUtc:yyyyMMddHHmm}",
                "A hall suitable for conferences.",
                ResourceType.Venue,
                DateTime.UtcNow);

            var reservation = ResourceReservation.Create(
                eventItem.Id,
                resource.Id,
                startsAtUtc,
                startsAtUtc.AddHours(2),
                DateTime.UtcNow);

            DbContext.Resources.Add(resource);
            DbContext.ResourceReservations.Add(reservation);
            await DbContext.SaveChangesAsync();

            return reservation;
        }
    }
}
