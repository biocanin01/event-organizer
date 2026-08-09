using EventOrganizer.Application.Commands.RejectResourceReservation;
using EventOrganizer.Domain.Resources;
using Microsoft.EntityFrameworkCore;

namespace EventOrganizer.Tests.Application.Resources
{
    public sealed class RejectResourceReservationCommandHandlerTests : ApplicationTestBase
    {
        [Fact]
        public async Task Handle_WhenReservationIsPending_RejectsReservation()
        {
            var reservation = await CreateReservationAsync();
            var handler = new RejectResourceReservationCommandHandler(DbContext);

            await handler.Handle(
                new RejectResourceReservationCommand(reservation.Id),
                CancellationToken.None);

            DbContext.ChangeTracker.Clear();

            var rejectedReservation = await DbContext.ResourceReservations
                .SingleAsync(item => item.Id == reservation.Id);

            Assert.Equal(
                ResourceReservationStatus.Rejected,
                rejectedReservation.Status);
            Assert.NotNull(rejectedReservation.UpdatedAtUtc);
        }

        private async Task<ResourceReservation> CreateReservationAsync()
        {
            var eventItem = await CreateEventAsync();
            var resource = TestResourceFactory.Create(
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
