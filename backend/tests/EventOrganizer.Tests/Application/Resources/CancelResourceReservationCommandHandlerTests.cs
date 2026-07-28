using EventOrganizer.Application.Commands.CancelResourceReservation;
using EventOrganizer.Application.Common.Exceptions;
using EventOrganizer.Domain.Resources;
using Microsoft.EntityFrameworkCore;

namespace EventOrganizer.Tests.Application.Resources
{
    public sealed class CancelResourceReservationCommandHandlerTests : ApplicationTestBase
    {
        [Theory]
        [InlineData(ResourceReservationStatus.Pending)]
        [InlineData(ResourceReservationStatus.Confirmed)]
        public async Task Handle_WhenReservationCanBeCancelled_CancelsReservation(
            ResourceReservationStatus status)
        {
            var reservation = await CreateReservationAsync(status);
            var handler = new CancelResourceReservationCommandHandler(DbContext);

            await handler.Handle(
                new CancelResourceReservationCommand(reservation.Id),
                CancellationToken.None);

            DbContext.ChangeTracker.Clear();

            var cancelledReservation = await DbContext.ResourceReservations
                .SingleAsync(item => item.Id == reservation.Id);

            Assert.Equal(
                ResourceReservationStatus.Cancelled,
                cancelledReservation.Status);
            Assert.NotNull(cancelledReservation.UpdatedAtUtc);
        }

        [Fact]
        public async Task Handle_WhenReservationDoesNotExist_ThrowsNotFoundException()
        {
            var handler = new CancelResourceReservationCommandHandler(DbContext);

            var action = () => handler.Handle(
                new CancelResourceReservationCommand(Guid.NewGuid()),
                CancellationToken.None);

            await Assert.ThrowsAsync<NotFoundException>(action);
        }

        [Fact]
        public async Task Handle_WhenReservationIsRejected_ThrowsInvalidOperationException()
        {
            var reservation = await CreateReservationAsync(ResourceReservationStatus.Rejected);
            var handler = new CancelResourceReservationCommandHandler(DbContext);

            var action = () => handler.Handle(
                new CancelResourceReservationCommand(reservation.Id),
                CancellationToken.None);

            await Assert.ThrowsAsync<InvalidOperationException>(action);
        }

        private async Task<ResourceReservation> CreateReservationAsync(
            ResourceReservationStatus status)
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

            if (status == ResourceReservationStatus.Confirmed)
            {
                reservation.Confirm(DateTime.UtcNow);
            }
            else if (status == ResourceReservationStatus.Rejected)
            {
                reservation.Reject(DateTime.UtcNow);
            }

            DbContext.Resources.Add(resource);
            DbContext.ResourceReservations.Add(reservation);
            await DbContext.SaveChangesAsync();

            return reservation;
        }
    }
}
