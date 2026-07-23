using EventOrganizer.Application.Common.Exceptions;
using EventOrganizer.Application.Common.Interfaces;
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

            var hasActiveReservations = await _dbContext.ResourceReservations
                .AnyAsync(
                    reservation =>
                        reservation.ResourceId == request.ResourceId
                        && reservation.EndsAtUtc > now
                        && (reservation.Status == ResourceReservationStatus.Pending
                            || reservation.Status == ResourceReservationStatus.Confirmed),
                    cancellationToken);

            if (hasActiveReservations)
            {
                throw new ConflictException(
                    "Resource cannot be archived while it has active or future reservations.");
            }

            resource.Archive(now);

            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}
