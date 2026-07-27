using EventOrganizer.Application.Common.Exceptions;
using EventOrganizer.Application.Common.Interfaces;
using EventOrganizer.Domain.Resources;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EventOrganizer.Application.Commands.ConfirmResourceReservation
{
    public sealed class ConfirmResourceReservationCommandHandler
        : IRequestHandler<ConfirmResourceReservationCommand>
    {
        private readonly IApplicationDbContext _dbContext;

        public ConfirmResourceReservationCommandHandler(IApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task Handle(
            ConfirmResourceReservationCommand request,
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

            reservation.Confirm(DateTime.UtcNow);

            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}
