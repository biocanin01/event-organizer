using EventOrganizer.Application.Common.Interfaces;
using EventOrganizer.Application.Responses;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EventOrganizer.Application.Queries.ListResourceReservations
{
    public sealed class ListResourceReservationsQueryHandler
        : IRequestHandler<ListResourceReservationsQuery, IReadOnlyList<ResourceReservationResponse>>
    {
        private readonly IApplicationDbContext _dbContext;

        public ListResourceReservationsQueryHandler(IApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<IReadOnlyList<ResourceReservationResponse>> Handle(
            ListResourceReservationsQuery request,
            CancellationToken cancellationToken)
        {
            return await _dbContext.ResourceReservations
                .AsNoTracking()
                .OrderBy(reservation => reservation.StartsAtUtc)
                .Select(reservation => new ResourceReservationResponse(
                    reservation.Id,
                    reservation.EventId,
                    reservation.ResourceId,
                    reservation.StartsAtUtc,
                    reservation.EndsAtUtc,
                    reservation.Status.ToString(),
                    reservation.CreatedAtUtc,
                    reservation.UpdatedAtUtc))
                .ToListAsync(cancellationToken);
        }
    }
}
