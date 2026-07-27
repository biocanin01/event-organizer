using EventOrganizer.Application.Commands.ConfirmResourceReservation;
using EventOrganizer.Application.Common.Exceptions;
using EventOrganizer.Domain.Resources;
using Microsoft.EntityFrameworkCore;

namespace EventOrganizer.Tests.Application.Resources
{
    public sealed class ConfirmResourceReservationCommandHandlerTests : ApplicationTestBase
    {
        [Fact]
        public async Task Handle_WhenReservationIsPending_ConfirmsReservation()
        {
            var reservation = await CreateReservationAsync();
            var handler = new ConfirmResourceReservationCommandHandler(DbContext);

            await handler.Handle(
                new ConfirmResourceReservationCommand(reservation.Id),
                CancellationToken.None);

            DbContext.ChangeTracker.Clear();

            var confirmedReservation = await DbContext.ResourceReservations
                .SingleAsync(item => item.Id == reservation.Id);

            Assert.Equal(
                ResourceReservationStatus.Confirmed,
                confirmedReservation.Status);
            Assert.NotNull(confirmedReservation.UpdatedAtUtc);
        }

        [Fact]
        public async Task Handle_WhenReservationDoesNotExist_ThrowsNotFoundException()
        {
            var handler = new ConfirmResourceReservationCommandHandler(DbContext);

            var action = () => handler.Handle(
                new ConfirmResourceReservationCommand(Guid.NewGuid()),
                CancellationToken.None);

            await Assert.ThrowsAsync<NotFoundException>(action);
        }

        [Fact]
        public async Task Handle_WhenReservationIsRejected_ThrowsInvalidOperationException()
        {
            var reservation = await CreateReservationAsync();
            reservation.Reject(DateTime.UtcNow);
            await DbContext.SaveChangesAsync();

            var handler = new ConfirmResourceReservationCommandHandler(DbContext);

            var action = () => handler.Handle(
                new ConfirmResourceReservationCommand(reservation.Id),
                CancellationToken.None);

            await Assert.ThrowsAsync<InvalidOperationException>(action);
        }

        private async Task<ResourceReservation> CreateReservationAsync()
        {
            var eventItem = await CreateEventAsync();
            var resource = Resource.Create(
                "Main Conference Hall",
                "A hall suitable for conferences.",
                ResourceType.Venue,
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
