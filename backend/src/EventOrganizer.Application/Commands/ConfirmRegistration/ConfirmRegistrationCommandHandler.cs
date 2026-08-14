using EventOrganizer.Application.Common.Authorization;
using EventOrganizer.Application.Common.Exceptions;
using EventOrganizer.Application.Common.Interfaces;
using EventOrganizer.Application.Registrations;
using EventOrganizer.Application.Responses;
using EventOrganizer.Domain.Events;
using EventOrganizer.Domain.Registrations;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Data;

namespace EventOrganizer.Application.Commands.ConfirmRegistration
{
    public sealed class ConfirmRegistrationCommandHandler
        : IRequestHandler<ConfirmRegistrationCommand, RegistrationResponse>
    {
        private readonly IApplicationDbContext _dbContext;
        private readonly ICurrentUserService _currentUserService;
        private readonly IUserManagementService _userManagementService;
        private readonly EventAuthorizationService _eventAuthorizationService;

        public ConfirmRegistrationCommandHandler(
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
            ConfirmRegistrationCommand request,
            CancellationToken cancellationToken)
        {
            await using var transaction = await _dbContext.BeginTransactionAsync(
                IsolationLevel.Serializable,
                cancellationToken);

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

            var confirmedCount = await _dbContext.Registrations.CountAsync(
                current => current.EventId == eventItem.Id
                    && current.Status == RegistrationStatus.Confirmed,
                cancellationToken);

            if (confirmedCount >= eventItem.Capacity)
            {
                throw new ConflictException("The event capacity has been reached.");
            }

            try
            {
                registration.Confirm(decisionUserId, DateTime.UtcNow);
                await _dbContext.SaveChangesAsync(cancellationToken);
                await _dbContext.CommitTransactionAsync(transaction, cancellationToken);
            }
            catch (InvalidOperationException exception)
            {
                throw new ConflictException(exception.Message, exception);
            }
            catch (DbUpdateConcurrencyException exception)
            {
                throw new ConflictException(
                    "The registration or event capacity changed. Refresh and try again.",
                    exception);
            }

            return await RegistrationResponseFactory.CreateAsync(
                _dbContext,
                _userManagementService,
                registration,
                cancellationToken);
        }
    }
}
