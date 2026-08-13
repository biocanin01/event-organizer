using EventOrganizer.Application.Common.Authorization;
using EventOrganizer.Application.Common.Exceptions;
using EventOrganizer.Application.Common.Interfaces;
using EventOrganizer.Domain.Bookings;
using EventOrganizer.Domain.Events;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EventOrganizer.Application.Commands.UpdateEvent
{
    public sealed class UpdateEventCommandHandler : IRequestHandler<UpdateEventCommand>
    {
        private readonly IApplicationDbContext _dbContext;
        private readonly EventAuthorizationService _eventAuthorizationService;

        public UpdateEventCommandHandler(
            IApplicationDbContext dbContext,
            EventAuthorizationService eventAuthorizationService)
        {
            _dbContext = dbContext;
            _eventAuthorizationService = eventAuthorizationService;
        }

        public async Task Handle(
            UpdateEventCommand request,
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

            if (eventItem.Status != EventStatus.Draft)
            {
                throw new ConflictException("Only draft events can be updated.");
            }

            var booking = await _dbContext.EventResourceBookings
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    booking => booking.EventId == request.EventId,
                    cancellationToken);

            if (booking is null)
            {
                throw new ConflictException("Event must have a draft booking before it can be updated.");
            }

            if (booking.Status != EventResourceBookingStatus.Draft)
            {
                throw new ConflictException(
                    "Event details can be updated only while the booking is draft.");
            }

            try
            {
                eventItem.UpdateDetails(
                    request.Title,
                    request.Description,
                    request.StartsAtUtc,
                    request.EndsAtUtc,
                    request.Capacity,
                    request.Budget,
                    request.Area,
                    request.RequiredSpeakerCount,
                    request.RequiresEquipment,
                    DateTime.UtcNow);
            }
            catch (ArgumentException exception)
            {
                throw new ConflictException(exception.Message, exception);
            }
            catch (InvalidOperationException exception)
            {
                throw new ConflictException(exception.Message, exception);
            }

            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}
