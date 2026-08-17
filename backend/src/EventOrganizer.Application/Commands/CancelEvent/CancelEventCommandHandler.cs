using EventOrganizer.Application.Common.Authorization;
using EventOrganizer.Application.Common.Exceptions;
using EventOrganizer.Application.Common.Interfaces;
using EventOrganizer.Application.Notifications;
using EventOrganizer.Domain.Bookings;
using EventOrganizer.Domain.Events;
using EventOrganizer.Domain.Registrations;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EventOrganizer.Application.Commands.CancelEvent
{
    public sealed class CancelEventCommandHandler : IRequestHandler<CancelEventCommand>
    {
        private readonly IApplicationDbContext _dbContext;
        private readonly EventAuthorizationService _eventAuthorizationService;
        private readonly INotificationService _notificationService;

        public CancelEventCommandHandler(
            IApplicationDbContext dbContext,
            EventAuthorizationService eventAuthorizationService,
            INotificationService notificationService)
        {
            _dbContext = dbContext;
            _eventAuthorizationService = eventAuthorizationService;
            _notificationService = notificationService;
        }

        public async Task Handle(
            CancelEventCommand request,
            CancellationToken cancellationToken)
        {
            var eventItem = await _dbContext.Events
                .FirstOrDefaultAsync(
                    eventItem => eventItem.Id == request.EventId,
                    cancellationToken);

            if (eventItem is null)
            {
                throw new NotFoundException(nameof(Event), request.EventId);
            }

            _eventAuthorizationService.EnsureCanManage(eventItem);

            var now = DateTime.UtcNow;

            eventItem.Cancel(now);

            var booking = await _dbContext.EventResourceBookings
                .FirstOrDefaultAsync(
                    booking => booking.EventId == request.EventId,
                    cancellationToken);

            if (booking?.Status is EventResourceBookingStatus.Draft
                or EventResourceBookingStatus.Submitted
                or EventResourceBookingStatus.Approved)
            {
                booking.Cancel(now);
            }

            var activeRegistrations = await _dbContext.Registrations
                .Where(registration =>
                    registration.EventId == request.EventId
                    && (registration.Status == RegistrationStatus.Pending
                        || registration.Status == RegistrationStatus.Confirmed))
                .ToArrayAsync(cancellationToken);

            foreach (var registration in activeRegistrations)
            {
                registration.Cancel(now);
            }

            _notificationService.AddEventCancelled(
                activeRegistrations.Select(registration => registration.ParticipantUserId),
                eventItem.Id,
                eventItem.Title,
                now);

            try
            {
                await _dbContext.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateConcurrencyException exception)
            {
                throw new ConflictException(
                    "The event or one of its registrations has changed. Refresh and try again.",
                    exception);
            }
        }
    }
}
