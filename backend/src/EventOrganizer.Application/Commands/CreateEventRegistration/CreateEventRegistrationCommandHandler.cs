using EventOrganizer.Application.Common.Exceptions;
using EventOrganizer.Application.Common.Interfaces;
using EventOrganizer.Application.Registrations;
using EventOrganizer.Application.Responses;
using EventOrganizer.Domain.Events;
using EventOrganizer.Domain.Registrations;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EventOrganizer.Application.Commands.CreateEventRegistration
{
    public sealed class CreateEventRegistrationCommandHandler
        : IRequestHandler<CreateEventRegistrationCommand, RegistrationResponse>
    {
        private readonly IApplicationDbContext _dbContext;
        private readonly ICurrentUserService _currentUserService;
        private readonly IUserManagementService _userManagementService;

        public CreateEventRegistrationCommandHandler(
            IApplicationDbContext dbContext,
            ICurrentUserService currentUserService,
            IUserManagementService userManagementService)
        {
            _dbContext = dbContext;
            _currentUserService = currentUserService;
            _userManagementService = userManagementService;
        }

        public async Task<RegistrationResponse> Handle(
            CreateEventRegistrationCommand request,
            CancellationToken cancellationToken)
        {
            var participantUserId = RegistrationGuards.RequireParticipant(_currentUserService);
            var eventItem = await _dbContext.Events.FirstOrDefaultAsync(
                eventItem => eventItem.Id == request.EventId,
                cancellationToken);

            if (eventItem is null)
            {
                throw new NotFoundException(nameof(Event), request.EventId);
            }

            var now = DateTime.UtcNow;
            if (eventItem.Status != EventStatus.Published || eventItem.StartsAtUtc <= now)
            {
                throw new ConflictException("Registration is only available for future published events.");
            }

            if (eventItem.OrganizerUserId == participantUserId)
            {
                throw new ConflictException("Event organizers cannot register for their own event.");
            }

            if (await _dbContext.Registrations.AnyAsync(
                registration => registration.EventId == eventItem.Id
                    && registration.ParticipantUserId == participantUserId,
                cancellationToken))
            {
                throw new ConflictException("A registration for this event already exists.");
            }

            var registration = Registration.Create(eventItem.Id, participantUserId, now);
            _dbContext.Registrations.Add(registration);

            try
            {
                await _dbContext.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException exception)
            {
                throw new ConflictException("A registration for this event already exists.", exception);
            }

            return await RegistrationResponseFactory.CreateAsync(
                _dbContext,
                _userManagementService,
                registration,
                cancellationToken);
        }
    }
}
