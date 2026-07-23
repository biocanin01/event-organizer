using EventOrganizer.Application.Commands.ArchiveResource;
using EventOrganizer.Application.Common.Exceptions;
using EventOrganizer.Domain.Resources;
using Microsoft.EntityFrameworkCore;

namespace EventOrganizer.Tests.Application.Resources
{
    public sealed class ArchiveResourceCommandHandlerTests : ApplicationTestBase
    {
        [Fact]
        public async Task Handle_WhenResourceExists_ArchivesResource()
        {
            var resource = await CreateResourceAsync();
            var handler = new ArchiveResourceCommandHandler(DbContext);
            var command = new ArchiveResourceCommand(resource.Id);

            await handler.Handle(command, CancellationToken.None);

            DbContext.ChangeTracker.Clear();

            var archivedResource = await DbContext.Resources
                .SingleAsync(resource => resource.Id == command.ResourceId);

            Assert.Equal(ResourceStatus.Archived, archivedResource.Status);
            Assert.NotNull(archivedResource.UpdatedAtUtc);
        }

        [Fact]
        public async Task Handle_WhenResourceDoesNotExist_ThrowsNotFoundException()
        {
            var handler = new ArchiveResourceCommandHandler(DbContext);

            var action = () => handler.Handle(
                new ArchiveResourceCommand(Guid.NewGuid()),
                CancellationToken.None);

            await Assert.ThrowsAsync<NotFoundException>(action);
        }

        [Fact]
        public async Task Handle_WhenResourceHasFuturePendingReservation_ThrowsConflictException()
        {
            var resource = await CreateResourceAsync();
            await CreateReservationAsync(resource.Id, ResourceReservationStatus.Pending);
            var handler = new ArchiveResourceCommandHandler(DbContext);

            var action = () => handler.Handle(
                new ArchiveResourceCommand(resource.Id),
                CancellationToken.None);

            await Assert.ThrowsAsync<ConflictException>(action);
        }

        [Fact]
        public async Task Handle_WhenResourceHasFutureConfirmedReservation_ThrowsConflictException()
        {
            var resource = await CreateResourceAsync();
            await CreateReservationAsync(resource.Id, ResourceReservationStatus.Confirmed);
            var handler = new ArchiveResourceCommandHandler(DbContext);

            var action = () => handler.Handle(
                new ArchiveResourceCommand(resource.Id),
                CancellationToken.None);

            await Assert.ThrowsAsync<ConflictException>(action);
        }

        [Theory]
        [InlineData(ResourceReservationStatus.Rejected)]
        [InlineData(ResourceReservationStatus.Cancelled)]
        public async Task Handle_WhenResourceHasNonBlockingReservation_ArchivesResource(
            ResourceReservationStatus status)
        {
            var resource = await CreateResourceAsync();
            await CreateReservationAsync(resource.Id, status);
            var handler = new ArchiveResourceCommandHandler(DbContext);

            await handler.Handle(
                new ArchiveResourceCommand(resource.Id),
                CancellationToken.None);

            Assert.Equal(ResourceStatus.Archived, resource.Status);
        }

        private async Task<Resource> CreateResourceAsync()
        {
            var resource = Resource.Create(
                "Projector",
                "Conference room projector.",
                ResourceType.Equipment,
                DateTime.UtcNow);

            DbContext.Resources.Add(resource);
            await DbContext.SaveChangesAsync();

            return resource;
        }

        private async Task CreateReservationAsync(
            Guid resourceId,
            ResourceReservationStatus status)
        {
            var eventEntity = await CreateEventAsync(
                startsAtUtc: DateTime.UtcNow.AddDays(10));

            var reservation = ResourceReservation.Create(
                eventEntity.Id,
                resourceId,
                DateTime.UtcNow.AddDays(10),
                DateTime.UtcNow.AddDays(10).AddHours(2),
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
    }
}
