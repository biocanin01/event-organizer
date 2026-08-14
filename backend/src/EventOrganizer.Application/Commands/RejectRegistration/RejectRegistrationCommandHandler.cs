using EventOrganizer.Application.Common.Authorization;
using EventOrganizer.Application.Common.Exceptions;
using EventOrganizer.Application.Common.Interfaces;
using EventOrganizer.Application.Registrations;
using EventOrganizer.Application.Responses;
using EventOrganizer.Domain.Events;
using EventOrganizer.Domain.Registrations;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EventOrganizer.Application.Commands.RejectRegistration
{
    public sealed class RejectRegistrationCommandHandler
        : IRequestHandler<RejectRegistrationCommand, RegistrationResponse>
    {
        private readonly IApplicationDbContext _dbContext;
        private readonly ICurrentUserService _currentUserService;
        private readonly IUserManagementService _userManagementService;
        private readonly EventAuthorizationService _eventAuthorizationService;

        public RejectRegistrationCommandHandler(
            IApplicationDbContext dbContext,
            ICurrentUserService currentUserService,
            IUserManagementService userManagementService,
            EventAuthorizationService eventAuthorizationService)
        {
            _dbContext = dbContext;
            _currentUserService = currentUserService;
            _userManagementService = userManagementService;
            _eventAuthorizationService = eventAuthorizationService;
        }

        public async Task<RegistrationResponse> Handle(
            RejectRegistrationCommand request,
            CancellationToken cancellationToken)
        {
            var registration = await _dbContext.Registrations.FirstOrDefaultAsync(
                registration => registration.Id == request.RegistrationId,
                cancellationToken);

            if (registration is null)
            {
                throw new NotFoundException(nameof(Registration), request.RegistrationId);
            }

            var eventItem = await _dbContext.Events.FirstOrDefaultAsync(
                eventItem => eventItem.Id == registration.EventId,
                cancellationToken);

            if (eventItem is null)
            {
                throw new NotFoundException(nameof(Event), registration.EventId);
            }

            var decisionUserId = RegistrationGuards.RequireEventManager(
                eventItem,
                _currentUserService,
                _eventAuthorizationService);
            RegistrationGuards.EnsureExpectedVersion(registration, request.Version);

            try
            {
                registration.Reject(request.Reason, decisionUserId, DateTime.UtcNow);
                await _dbContext.SaveChangesAsync(cancellationToken);
            }
            catch (InvalidOperationException exception)
            {
                throw new ConflictException(exception.Message, exception);
            }
            catch (DbUpdateConcurrencyException exception)
            {
                throw new ConflictException("The registration has changed. Refresh it and try again.", exception);
            }

            return await RegistrationResponseFactory.CreateAsync(
                _dbContext,
                _userManagementService,
                registration,
                cancellationToken);
        }
    }
}
