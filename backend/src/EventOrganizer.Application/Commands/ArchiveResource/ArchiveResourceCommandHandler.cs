using EventOrganizer.Application.Common.Exceptions;
using EventOrganizer.Application.Common.Interfaces;
using EventOrganizer.Domain.Bookings;
using EventOrganizer.Domain.Resources;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EventOrganizer.Application.Commands.ArchiveResource
{
    public sealed class ArchiveResourceCommandHandler
        : IRequestHandler<ArchiveResourceCommand>
    {
        private readonly IApplicationDbContext _dbContext;

        public ArchiveResourceCommandHandler(IApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task Handle(
            ArchiveResourceCommand request,
            CancellationToken cancellationToken)
        {
            var resource = await _dbContext.Resources
                .FirstOrDefaultAsync(
                    resource => resource.Id == request.ResourceId,
                    cancellationToken);

            if (resource is null)
            {
                throw new NotFoundException(nameof(Resource), request.ResourceId);
            }

            var now = DateTime.UtcNow;

            var hasActiveBookings = await _dbContext.EventResourceBookings
                .AnyAsync(
                    booking =>
                        (booking.Status == EventResourceBookingStatus.Approved
                            || (booking.Status == EventResourceBookingStatus.Submitted
                                && booking.HoldExpiresAtUtc > now))
                        && booking.Items.Any(item => item.ResourceId == request.ResourceId),
                    cancellationToken);

            if (hasActiveBookings)
            {
                throw new ConflictException(
                    "Resource cannot be archived while it belongs to an active booking.");
            }

            resource.Archive(now);

            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}
