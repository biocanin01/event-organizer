using EventOrganizer.Application.Commands.ArchiveResource;
using EventOrganizer.Application.Common.Exceptions;
using EventOrganizer.Domain.Bookings;
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

        [Theory]
        [InlineData(EventResourceBookingStatus.Submitted)]
        [InlineData(EventResourceBookingStatus.Approved)]
        public async Task Handle_WhenResourceBelongsToActiveBooking_ThrowsConflictException(
            EventResourceBookingStatus status)
        {
            var resource = await CreateResourceAsync();
            await CreateBookingWithStatusAsync(resource, status);
            var handler = new ArchiveResourceCommandHandler(DbContext);

            var action = () => handler.Handle(
                new ArchiveResourceCommand(resource.Id),
                CancellationToken.None);

            await Assert.ThrowsAsync<ConflictException>(action);
        }

        [Theory]
        [InlineData(EventResourceBookingStatus.Draft)]
        [InlineData(EventResourceBookingStatus.Rejected)]
        [InlineData(EventResourceBookingStatus.Expired)]
        [InlineData(EventResourceBookingStatus.Cancelled)]
        public async Task Handle_WhenResourceBelongsToNonBlockingBooking_ArchivesResource(
            EventResourceBookingStatus status)
        {
            var resource = await CreateResourceAsync();
            await CreateBookingWithStatusAsync(resource, status);
            var handler = new ArchiveResourceCommandHandler(DbContext);

            await handler.Handle(
                new ArchiveResourceCommand(resource.Id),
                CancellationToken.None);

            DbContext.ChangeTracker.Clear();

            var archivedResource = await DbContext.Resources
                .SingleAsync(item => item.Id == resource.Id);

            Assert.Equal(ResourceStatus.Archived, archivedResource.Status);
        }

        private async Task<Resource> CreateResourceAsync()
        {
            var resource = TestResourceFactory.Create(
                "Projector",
                "Conference room projector.",
                ResourceType.EquipmentPackage,
                100m,
                null,
                null,
                3,
                DateTime.UtcNow);

            DbContext.Resources.Add(resource);
            await DbContext.SaveChangesAsync();

            return resource;
        }

        private async Task CreateBookingWithStatusAsync(
            Resource resource,
            EventResourceBookingStatus status)
        {
            var eventEntity = await CreateEventAsync(
                startsAtUtc: DateTime.UtcNow.AddDays(10));
            var booking = await CreateBookingAsync(eventEntity, resource);

            if (status != EventResourceBookingStatus.Draft)
            {
                await SetBookingStatusAsync(booking.Id, status);
            }
        }
    }
}
