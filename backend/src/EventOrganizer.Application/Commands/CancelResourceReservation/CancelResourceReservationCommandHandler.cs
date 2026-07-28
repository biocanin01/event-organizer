using EventOrganizer.Application.Common.Exceptions;
using EventOrganizer.Application.Common.Interfaces;
using EventOrganizer.Domain.Resources;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EventOrganizer.Application.Commands.CancelResourceReservation
{
    public sealed class CancelResourceReservationCommandHandler
        : IRequestHandler<CancelResourceReservationCommand>
    {
        private readonly IApplicationDbContext _dbContext;

        public CancelResourceReservationCommandHandler(IApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task Handle(
            CancelResourceReservationCommand request,
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

            reservation.Cancel(DateTime.UtcNow);

            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}
