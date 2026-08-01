using EventOrganizer.Application.Commands.CreateResourceReservation;
using EventOrganizer.Application.Common.Exceptions;
using EventOrganizer.Domain.Resources;
using Microsoft.EntityFrameworkCore;

namespace EventOrganizer.Tests.Application.Resources
{
    public sealed class CreateResourceReservationCommandHandlerTests : ApplicationTestBase
    {
        [Fact]
        public async Task Handle_WhenRequestIsValid_CreatesPendingReservation()
        {
            var eventItem = await CreateEventAsync();
            var resource = await CreateResourceAsync();
            var handler = new CreateResourceReservationCommandHandler(DbContext);
            var command = CreateCommand(eventItem.Id, resource.Id);

            var reservationId = await handler.Handle(command, CancellationToken.None);

            var reservation = await DbContext.ResourceReservations
                .SingleAsync(reservation => reservation.Id == reservationId);

            Assert.Equal(eventItem.Id, reservation.EventId);
            Assert.Equal(resource.Id, reservation.ResourceId);
            Assert.Equal(ResourceReservationStatus.Pending, reservation.Status);
        }

        [Fact]
        public async Task Handle_WhenEventDoesNotExist_ThrowsNotFoundException()
        {
            var resource = await CreateResourceAsync();
            var handler = new CreateResourceReservationCommandHandler(DbContext);

            var action = () => handler.Handle(
                CreateCommand(Guid.NewGuid(), resource.Id),
                CancellationToken.None);

            await Assert.ThrowsAsync<NotFoundException>(action);
        }

        [Fact]
        public async Task Handle_WhenResourceDoesNotExist_ThrowsNotFoundException()
        {
            var eventItem = await CreateEventAsync();
            var handler = new CreateResourceReservationCommandHandler(DbContext);

            var action = () => handler.Handle(
                CreateCommand(eventItem.Id, Guid.NewGuid()),
                CancellationToken.None);

            await Assert.ThrowsAsync<NotFoundException>(action);
        }

        [Fact]
        public async Task Handle_WhenResourceIsArchived_ThrowsConflictException()
        {
            var eventItem = await CreateEventAsync();
            var resource = await CreateResourceAsync();
            resource.Archive(DateTime.UtcNow);
            await DbContext.SaveChangesAsync();

            var handler = new CreateResourceReservationCommandHandler(DbContext);

            var action = () => handler.Handle(
                CreateCommand(eventItem.Id, resource.Id),
                CancellationToken.None);

            await Assert.ThrowsAsync<ConflictException>(action);
        }

        [Theory]
        [InlineData(ResourceReservationStatus.Pending)]
        [InlineData(ResourceReservationStatus.Confirmed)]
        public async Task Handle_WhenReservationOverlapsWithBlockingStatus_ThrowsConflictException(
            ResourceReservationStatus status)
        {
            var eventItem = await CreateEventAsync();
            var resource = await CreateResourceAsync();
            await CreateExistingReservationAsync(eventItem.Id, resource.Id, status);

            var handler = new CreateResourceReservationCommandHandler(DbContext);

            var action = () => handler.Handle(
                CreateCommand(eventItem.Id, resource.Id),
                CancellationToken.None);

            await Assert.ThrowsAsync<ConflictException>(action);
        }

        [Theory]
        [InlineData(ResourceReservationStatus.Rejected)]
        [InlineData(ResourceReservationStatus.Cancelled)]
        public async Task Handle_WhenReservationOverlapsWithNonBlockingStatus_CreatesReservation(
            ResourceReservationStatus status)
        {
            var eventItem = await CreateEventAsync();
            var resource = await CreateResourceAsync();
            await CreateExistingReservationAsync(eventItem.Id, resource.Id, status);

            var handler = new CreateResourceReservationCommandHandler(DbContext);

            var reservationId = await handler.Handle(
                CreateCommand(eventItem.Id, resource.Id),
                CancellationToken.None);

            Assert.NotEqual(Guid.Empty, reservationId);
        }

        private async Task<Resource> CreateResourceAsync()
        {
            var resource = Resource.Create(
                "Main Conference Hall",
                "A hall suitable for conferences.",
                ResourceType.Venue,
                500m,
                150,
                "IT",
                4,
                DateTime.UtcNow);

            DbContext.Resources.Add(resource);
            await DbContext.SaveChangesAsync();

            return resource;
        }

        private async Task CreateExistingReservationAsync(
            Guid eventId,
            Guid resourceId,
            ResourceReservationStatus status)
        {
            var reservation = ResourceReservation.Create(
                eventId,
                resourceId,
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
            else if (status == ResourceReservationStatus.Cancelled)
            {
                reservation.Cancel(DateTime.UtcNow);
            }

            DbContext.ResourceReservations.Add(reservation);
            await DbContext.SaveChangesAsync();
        }

        private static CreateResourceReservationCommand CreateCommand(
            Guid eventId,
            Guid resourceId)
        {
            return new CreateResourceReservationCommand(
                eventId,
                resourceId,
                new DateTime(2026, 9, 1, 10, 0, 0, DateTimeKind.Utc),
                new DateTime(2026, 9, 1, 12, 0, 0, DateTimeKind.Utc));
        }
    }
}
