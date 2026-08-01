using EventOrganizer.Application.Queries.GetResourceReservationById;
using EventOrganizer.Domain.Resources;

namespace EventOrganizer.Tests.Application.Resources
{
    public sealed class GetResourceReservationByIdQueryHandlerTests : ApplicationTestBase
    {
        [Fact]
        public async Task Handle_WhenReservationExists_ReturnsReservation()
        {
            var reservation = await CreateReservationAsync();
            var handler = new GetResourceReservationByIdQueryHandler(DbContext);

            var result = await handler.Handle(
                new GetResourceReservationByIdQuery(reservation.Id),
                CancellationToken.None);

            Assert.NotNull(result);
            Assert.Equal(reservation.Id, result.Id);
            Assert.Equal(reservation.EventId, result.EventId);
            Assert.Equal(reservation.ResourceId, result.ResourceId);
            Assert.Equal(reservation.Status.ToString(), result.Status);
        }

        [Fact]
        public async Task Handle_WhenReservationDoesNotExist_ReturnsNull()
        {
            var handler = new GetResourceReservationByIdQueryHandler(DbContext);

            var result = await handler.Handle(
                new GetResourceReservationByIdQuery(Guid.NewGuid()),
                CancellationToken.None);

            Assert.Null(result);
        }

        private async Task<ResourceReservation> CreateReservationAsync()
        {
            var eventItem = await CreateEventAsync();
            var resource = Resource.Create(
                "Main Conference Hall",
                "A hall suitable for conferences.",
                ResourceType.Venue,
                500m,
                150,
                "IT",
                4,
                DateTime.UtcNow);

            var reservation = ResourceReservation.Create(
                eventItem.Id,
                resource.Id,
                new DateTime(2026, 9, 1, 9, 0, 0, DateTimeKind.Utc),
                new DateTime(2026, 9, 1, 11, 0, 0, DateTimeKind.Utc),
                DateTime.UtcNow);

            DbContext.Resources.Add(resource);
            DbContext.ResourceReservations.Add(reservation);
            await DbContext.SaveChangesAsync();

            return reservation;
        }
    }
}
