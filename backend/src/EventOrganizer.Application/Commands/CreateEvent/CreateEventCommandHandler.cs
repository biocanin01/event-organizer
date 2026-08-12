using EventOrganizer.Application.Common.Exceptions;
using EventOrganizer.Application.Common.Interfaces;
using EventOrganizer.Domain.Bookings;
using EventOrganizer.Domain.Events;
using MediatR;

namespace EventOrganizer.Application.Commands.CreateEvent
{
    public sealed class CreateEventCommandHandler : IRequestHandler<CreateEventCommand, Guid>
    {
        private readonly IApplicationDbContext _dbContext;
        private readonly ICurrentUserService _currentUserService;

        public CreateEventCommandHandler(
            IApplicationDbContext dbContext,
            ICurrentUserService currentUserService)
        {
            _dbContext = dbContext;
            _currentUserService = currentUserService;
        }

        public async Task<Guid> Handle(CreateEventCommand request, CancellationToken cancellationToken)
        {
            if (!_currentUserService.IsAuthenticated || _currentUserService.UserId is null)
            {
                throw new UnauthorizedException("An authenticated user is required to create an event.");
            }

            var eventItem = Event.Create(
                request.Title,
                request.Description,
                request.StartsAtUtc,
                request.EndsAtUtc,
                request.Capacity,
                request.Budget,
                request.Area,
                request.RequiredSpeakerCount,
                _currentUserService.UserId.Value,
                DateTime.UtcNow,
                request.RequiresEquipment);

            var booking = EventResourceBooking.Create(eventItem.Id, DateTime.UtcNow);

            _dbContext.Events.Add(eventItem);
            _dbContext.EventResourceBookings.Add(booking);
            await _dbContext.SaveChangesAsync(cancellationToken);

            return eventItem.Id;
        }
    }
}
