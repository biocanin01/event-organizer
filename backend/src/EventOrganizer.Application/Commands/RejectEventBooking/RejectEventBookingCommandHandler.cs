using EventOrganizer.Application.Bookings;
using EventOrganizer.Application.Common.Exceptions;
using EventOrganizer.Application.Common.Interfaces;
using EventOrganizer.Application.Responses;
using EventOrganizer.Domain.Bookings;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EventOrganizer.Application.Commands.RejectEventBooking
{
    public sealed class RejectEventBookingCommandHandler
        : IRequestHandler<RejectEventBookingCommand, EventResourceBookingResponse>
    {
        private readonly IApplicationDbContext _dbContext;
        private readonly ICurrentUserService _currentUserService;

        public RejectEventBookingCommandHandler(
            IApplicationDbContext dbContext,
            ICurrentUserService currentUserService)
        {
            _dbContext = dbContext;
            _currentUserService = currentUserService;
        }

        public async Task<EventResourceBookingResponse> Handle(
            RejectEventBookingCommand request,
            CancellationToken cancellationToken)
        {
            var adminUserId = EventBookingAdminGuard.RequireAdminUserId(_currentUserService);
            var booking = await _dbContext.EventResourceBookings
                .Include(booking => booking.Items)
                .FirstOrDefaultAsync(
                    booking => booking.Id == request.BookingId,
                    cancellationToken);

            if (booking is null)
            {
                throw new NotFoundException(nameof(EventResourceBooking), request.BookingId);
            }

            EventBookingVersionGuard.EnsureExpectedVersion(booking, request.Version);
            try
            {
                booking.Reject(adminUserId, request.DecisionReason, DateTime.UtcNow);
            }
            catch (InvalidOperationException exception)
            {
                throw new ConflictException(exception.Message, exception);
            }

            try
            {
                await _dbContext.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateConcurrencyException exception)
            {
                throw new ConflictException(
                    "The booking has changed. Refresh it and try again.",
                    exception);
            }

            return await EventBookingResponseFactory.CreateAsync(
                _dbContext,
                booking,
                cancellationToken);
        }
    }
}
