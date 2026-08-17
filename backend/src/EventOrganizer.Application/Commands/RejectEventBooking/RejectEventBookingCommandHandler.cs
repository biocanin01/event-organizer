using EventOrganizer.Application.Bookings;
using EventOrganizer.Application.Common.Exceptions;
using EventOrganizer.Application.Common.Interfaces;
using EventOrganizer.Application.Notifications;
using EventOrganizer.Application.Responses;
using EventOrganizer.Domain.Bookings;
using EventOrganizer.Domain.Events;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EventOrganizer.Application.Commands.RejectEventBooking
{
    public sealed class RejectEventBookingCommandHandler
        : IRequestHandler<RejectEventBookingCommand, EventResourceBookingResponse>
    {
        private readonly IApplicationDbContext _dbContext;
        private readonly ICurrentUserService _currentUserService;
        private readonly INotificationService _notificationService;

        public RejectEventBookingCommandHandler(
            IApplicationDbContext dbContext,
            ICurrentUserService currentUserService,
            INotificationService notificationService)
        {
            _dbContext = dbContext;
            _currentUserService = currentUserService;
            _notificationService = notificationService;
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

            var eventItem = await _dbContext.Events.FirstOrDefaultAsync(
                eventItem => eventItem.Id == booking.EventId,
                cancellationToken);
            if (eventItem is null)
            {
                throw new NotFoundException(nameof(Event), booking.EventId);
            }

            EventBookingVersionGuard.EnsureExpectedVersion(booking, request.Version);
            var now = DateTime.UtcNow;
            try
            {
                booking.Reject(adminUserId, request.DecisionReason, now);
            }
            catch (InvalidOperationException exception)
            {
                throw new ConflictException(exception.Message, exception);
            }

            _notificationService.AddBookingRejected(
                eventItem.OrganizerUserId,
                eventItem.Id,
                eventItem.Title,
                booking.DecisionReason,
                now);

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
