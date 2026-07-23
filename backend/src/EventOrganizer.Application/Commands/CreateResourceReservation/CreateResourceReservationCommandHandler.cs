using EventOrganizer.Application.Common.Exceptions;
using EventOrganizer.Application.Common.Interfaces;
using EventOrganizer.Domain.Events;
using EventOrganizer.Domain.Resources;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EventOrganizer.Application.Commands.CreateResourceReservation
{
    public sealed class CreateResourceReservationCommandHandler
        : IRequestHandler<CreateResourceReservationCommand, Guid>
    {
        private readonly IApplicationDbContext _dbContext;

        public CreateResourceReservationCommandHandler(IApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<Guid> Handle(
            CreateResourceReservationCommand request,
            CancellationToken cancellationToken)
        {
            var eventExists = await _dbContext.Events
                .AnyAsync(eventItem => eventItem.Id == request.EventId, cancellationToken);

            if (!eventExists)
            {
                throw new NotFoundException(nameof(Event), request.EventId);
            }

            var resource = await _dbContext.Resources
                .FirstOrDefaultAsync(
                    resource => resource.Id == request.ResourceId,
                    cancellationToken);

            if (resource is null)
            {
                throw new NotFoundException(nameof(Resource), request.ResourceId);
            }

            if (resource.Status == ResourceStatus.Archived)
            {
                throw new ConflictException("Archived resources cannot be reserved.");
            }

            var hasOverlappingReservation = await _dbContext.ResourceReservations
                .AnyAsync(
                    reservation =>
                        reservation.ResourceId == request.ResourceId
                        && reservation.StartsAtUtc < request.EndsAtUtc
                        && reservation.EndsAtUtc > request.StartsAtUtc
                        && (reservation.Status == ResourceReservationStatus.Pending
                            || reservation.Status == ResourceReservationStatus.Confirmed),
                    cancellationToken);

            if (hasOverlappingReservation)
            {
                throw new ConflictException(
                    "Resource already has a pending or confirmed reservation for the requested time.");
            }

            var reservation = ResourceReservation.Create(
                request.EventId,
                request.ResourceId,
                request.StartsAtUtc,
                request.EndsAtUtc,
                DateTime.UtcNow);

            _dbContext.ResourceReservations.Add(reservation);

            await _dbContext.SaveChangesAsync(cancellationToken);

            return reservation.Id;
        }
    }
}