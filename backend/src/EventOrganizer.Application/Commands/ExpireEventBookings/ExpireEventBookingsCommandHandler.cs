using EventOrganizer.Application.Bookings;
using EventOrganizer.Application.Common.Exceptions;
using EventOrganizer.Application.Common.Interfaces;
using EventOrganizer.Application.Notifications;
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
        private readonly INotificationService _notificationService;

        public ExpireEventBookingsCommandHandler(
            IApplicationDbContext dbContext,
            ICurrentUserService currentUserService,
            INotificationService notificationService)
        {
            _dbContext = dbContext;
            _currentUserService = currentUserService;
            _notificationService = notificationService;
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

            var expiredBookings = new List<EventResourceBooking>();
            foreach (var booking in bookings)
            {
                if (booking.Expire(now))
                {
                    expiredBookings.Add(booking);
                }
            }

            if (expiredBookings.Count == 0)
            {
                return 0;
            }

            var eventIds = expiredBookings
                .Select(booking => booking.EventId)
                .Distinct()
                .ToArray();
            var events = await _dbContext.Events
                .Where(eventItem => eventIds.Contains(eventItem.Id))
                .ToDictionaryAsync(eventItem => eventItem.Id, cancellationToken);

            foreach (var booking in expiredBookings)
            {
                var eventItem = events[booking.EventId];
                _notificationService.AddBookingExpired(
                    eventItem.OrganizerUserId,
                    eventItem.Id,
                    eventItem.Title,
                    now);
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

            return expiredBookings.Count;
        }
    }
}
