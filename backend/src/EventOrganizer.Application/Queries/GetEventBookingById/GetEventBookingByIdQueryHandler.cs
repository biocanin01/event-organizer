using EventOrganizer.Application.Bookings;
using EventOrganizer.Application.Common.Exceptions;
using EventOrganizer.Application.Common.Interfaces;
using EventOrganizer.Application.Responses;
using EventOrganizer.Domain.Bookings;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EventOrganizer.Application.Queries.GetEventBookingById
{
    public sealed class GetEventBookingByIdQueryHandler
        : IRequestHandler<GetEventBookingByIdQuery, EventResourceBookingResponse>
    {
        private readonly IApplicationDbContext _dbContext;
        private readonly ICurrentUserService _currentUserService;

        public GetEventBookingByIdQueryHandler(
            IApplicationDbContext dbContext,
            ICurrentUserService currentUserService)
        {
            _dbContext = dbContext;
            _currentUserService = currentUserService;
        }

        public async Task<EventResourceBookingResponse> Handle(
            GetEventBookingByIdQuery request,
            CancellationToken cancellationToken)
        {
            EventBookingAdminGuard.RequireAdminUserId(_currentUserService);
            var booking = await _dbContext.EventResourceBookings
                .AsNoTracking()
                .Include(booking => booking.Items)
                .FirstOrDefaultAsync(
                    booking => booking.Id == request.BookingId,
                    cancellationToken);

            if (booking is null)
            {
                throw new NotFoundException(nameof(EventResourceBooking), request.BookingId);
            }

            return await EventBookingResponseFactory.CreateAsync(
                _dbContext,
                booking,
                cancellationToken);
        }
    }
}
