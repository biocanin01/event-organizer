using EventOrganizer.Application.Common.Interfaces;
using EventOrganizer.Application.Responses;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EventOrganizer.Application.Queries.GetResourceReservationById
{
    public sealed class GetResourceReservationByIdQueryHandler
        : IRequestHandler<GetResourceReservationByIdQuery, ResourceReservationResponse?>
    {
        private readonly IApplicationDbContext _dbContext;

        public GetResourceReservationByIdQueryHandler(IApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<ResourceReservationResponse?> Handle(
            GetResourceReservationByIdQuery request,
            CancellationToken cancellationToken)
        {
            return await _dbContext.ResourceReservations
                .AsNoTracking()
                .Where(reservation => reservation.Id == request.ReservationId)
                .Select(reservation => new ResourceReservationResponse(
                    reservation.Id,
                    reservation.EventId,
                    reservation.ResourceId,
                    reservation.StartsAtUtc,
                    reservation.EndsAtUtc,
                    reservation.Status.ToString(),
                    reservation.CreatedAtUtc,
                    reservation.UpdatedAtUtc))
                .FirstOrDefaultAsync(cancellationToken);
        }
    }
}
