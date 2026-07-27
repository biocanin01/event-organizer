using EventOrganizer.Application.Common.Exceptions;
using EventOrganizer.Application.Common.Interfaces;
using EventOrganizer.Domain.Resources;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EventOrganizer.Application.Commands.RejectResourceReservation
{
    public sealed class RejectResourceReservationCommandHandler
        : IRequestHandler<RejectResourceReservationCommand>
    {
        private readonly IApplicationDbContext _dbContext;

        public RejectResourceReservationCommandHandler(IApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task Handle(
            RejectResourceReservationCommand request,
            CancellationToken cancellationToken)
        {
            var reservation = await _dbContext.ResourceReservations
                .FirstOrDefaultAsync(
                    reservation => reservation.Id == request.ReservationId,
                    cancellationToken);

            if (reservation is null)
            {
                throw new NotFoundException(
                    nameof(ResourceReservation),
                    request.ReservationId);
            }

            reservation.Reject(DateTime.UtcNow);

            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}
