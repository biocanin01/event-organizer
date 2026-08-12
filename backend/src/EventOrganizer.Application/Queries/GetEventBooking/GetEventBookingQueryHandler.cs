using EventOrganizer.Application.Bookings;
using EventOrganizer.Application.Common.Authorization;
using EventOrganizer.Application.Common.Exceptions;
using EventOrganizer.Application.Common.Interfaces;
using EventOrganizer.Application.Responses;
using EventOrganizer.Domain.Bookings;
using EventOrganizer.Domain.Events;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EventOrganizer.Application.Queries.GetEventBooking
{
    public sealed class GetEventBookingQueryHandler
        : IRequestHandler<GetEventBookingQuery, EventResourceBookingResponse>
    {
        private readonly IApplicationDbContext _dbContext;
        private readonly EventAuthorizationService _authorizationService;

        public GetEventBookingQueryHandler(
            IApplicationDbContext dbContext,
            EventAuthorizationService authorizationService)
        {
            _dbContext = dbContext;
            _authorizationService = authorizationService;
        }

        public async Task<EventResourceBookingResponse> Handle(
            GetEventBookingQuery request,
            CancellationToken cancellationToken)
        {
            var eventItem = await _dbContext.Events
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    eventItem => eventItem.Id == request.EventId,
                    cancellationToken);

            if (eventItem is null)
            {
                throw new NotFoundException(nameof(Event), request.EventId);
            }

            _authorizationService.EnsureCanViewBooking(eventItem);

            var booking = await _dbContext.EventResourceBookings
                .AsNoTracking()
                .Include(booking => booking.Items)
                .FirstOrDefaultAsync(
                    booking => booking.EventId == request.EventId,
                    cancellationToken);

            if (booking is null)
            {
                throw new NotFoundException(nameof(EventResourceBooking), request.EventId);
            }

            return await EventBookingResponseFactory.CreateAsync(
                _dbContext,
                booking,
                cancellationToken);
        }
    }
}
