using EventOrganizer.Application.Bookings;
using EventOrganizer.Application.Common.Exceptions;
using EventOrganizer.Application.Common.Interfaces;
using EventOrganizer.Domain.Bookings;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EventOrganizer.Application.Commands.ExpireEventBookings
{
    public sealed class ExpireEventBookingsCommandHandler
        : IRequestHandler<ExpireEventBookingsCommand, int>
    {
        private readonly IApplicationDbContext _dbContext;
        private readonly ICurrentUserService _currentUserService;

        public ExpireEventBookingsCommandHandler(
            IApplicationDbContext dbContext,
            ICurrentUserService currentUserService)
        {
            _dbContext = dbContext;
            _currentUserService = currentUserService;
        }

        public async Task<int> Handle(
            ExpireEventBookingsCommand request,
            CancellationToken cancellationToken)
        {
            EventBookingAdminGuard.RequireAdminUserId(_currentUserService);
            var now = DateTime.UtcNow;
            var bookings = await _dbContext.EventResourceBookings
                .Where(booking =>
                    booking.Status == EventResourceBookingStatus.Submitted
                    && booking.HoldExpiresAtUtc <= now)
                .ToArrayAsync(cancellationToken);

            var expiredCount = 0;
            foreach (var booking in bookings)
            {
                if (booking.Expire(now))
                {
                    expiredCount++;
                }
            }

            if (expiredCount == 0)
            {
                return 0;
            }

            try
            {
                await _dbContext.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateConcurrencyException exception)
            {
                throw new ConflictException(
                    "One or more bookings changed while expiration was running. Please try again.",
                    exception);
            }

            return expiredCount;
        }
    }
}
