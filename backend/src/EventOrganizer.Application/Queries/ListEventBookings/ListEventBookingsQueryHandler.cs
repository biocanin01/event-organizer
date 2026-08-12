using EventOrganizer.Application.Bookings;
using EventOrganizer.Application.Common.Interfaces;
using EventOrganizer.Application.Responses;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EventOrganizer.Application.Queries.ListEventBookings
{
    public sealed class ListEventBookingsQueryHandler
        : IRequestHandler<ListEventBookingsQuery, IReadOnlyList<EventResourceBookingResponse>>
    {
        private readonly IApplicationDbContext _dbContext;
        private readonly ICurrentUserService _currentUserService;

        public ListEventBookingsQueryHandler(
            IApplicationDbContext dbContext,
            ICurrentUserService currentUserService)
        {
            _dbContext = dbContext;
            _currentUserService = currentUserService;
        }

        public async Task<IReadOnlyList<EventResourceBookingResponse>> Handle(
            ListEventBookingsQuery request,
            CancellationToken cancellationToken)
        {
            EventBookingAdminGuard.RequireAdminUserId(_currentUserService);
            var query = _dbContext.EventResourceBookings
                .AsNoTracking()
                .Include(booking => booking.Items)
                .AsQueryable();

            if (request.Status.HasValue)
            {
                query = query.Where(booking => booking.Status == request.Status.Value);
            }

            var bookings = await query
                .OrderByDescending(booking => booking.SubmittedAtUtc ?? booking.CreatedAtUtc)
                .ToArrayAsync(cancellationToken);

            return await EventBookingResponseFactory.CreateManyAsync(
                _dbContext,
                bookings,
                cancellationToken);
        }
    }
}
