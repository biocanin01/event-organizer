using EventOrganizer.Application.Common.Authorization;
using EventOrganizer.Application.Common.Exceptions;
using EventOrganizer.Application.Common.Interfaces;
using EventOrganizer.Domain.Events;
using EventOrganizer.Domain.Resources;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EventOrganizer.Application.Commands.CancelResourceReservation
{
    public sealed class CancelResourceReservationCommandHandler
        : IRequestHandler<CancelResourceReservationCommand>
    {
        private readonly IApplicationDbContext _dbContext;
        private readonly ResourceReservationAuthorizationService _authorizationService;

        public CancelResourceReservationCommandHandler(
            IApplicationDbContext dbContext,
            ResourceReservationAuthorizationService authorizationService)
        {
            _dbContext = dbContext;
            _authorizationService = authorizationService;
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

            var eventItem = await _dbContext.Events
                .FirstOrDefaultAsync(
                    eventItem => eventItem.Id == reservation.EventId,
                    cancellationToken);

            if (eventItem is null)
            {
                throw new NotFoundException(nameof(Event), reservation.EventId);
            }

            _authorizationService.EnsureCanCancel(reservation, eventItem);

            reservation.Cancel(DateTime.UtcNow);

            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}
