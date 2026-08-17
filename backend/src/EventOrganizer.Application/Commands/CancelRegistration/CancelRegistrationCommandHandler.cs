using EventOrganizer.Application.Common.Exceptions;
using EventOrganizer.Application.Common.Interfaces;
using EventOrganizer.Application.Notifications;
using EventOrganizer.Application.Registrations;
using EventOrganizer.Application.Responses;
using EventOrganizer.Domain.Registrations;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EventOrganizer.Application.Commands.CancelRegistration
{
    public sealed class CancelRegistrationCommandHandler
        : IRequestHandler<CancelRegistrationCommand, RegistrationResponse>
    {
        private readonly IApplicationDbContext _dbContext;
        private readonly ICurrentUserService _currentUserService;
        private readonly IUserManagementService _userManagementService;
        private readonly INotificationService _notificationService;

        public CancelRegistrationCommandHandler(
            IApplicationDbContext dbContext,
            ICurrentUserService currentUserService,
            IUserManagementService userManagementService,
            INotificationService notificationService)
        {
            _dbContext = dbContext;
            _currentUserService = currentUserService;
            _userManagementService = userManagementService;
            _notificationService = notificationService;
        }

        public async Task<RegistrationResponse> Handle(
            CancelRegistrationCommand request,
            CancellationToken cancellationToken)
        {
            var userId = RegistrationGuards.RequireAuthenticatedUser(_currentUserService);
            var registration = await _dbContext.Registrations.FirstOrDefaultAsync(
                registration => registration.Id == request.RegistrationId,
                cancellationToken);

            if (registration is null)
            {
                throw new NotFoundException(nameof(Registration), request.RegistrationId);
            }

            RegistrationGuards.EnsureOwner(registration, userId);
            RegistrationGuards.EnsureExpectedVersion(registration, request.Version);
            var eventItem = await _dbContext.Events.FirstAsync(
                eventItem => eventItem.Id == registration.EventId,
                cancellationToken);
            var now = DateTime.UtcNow;

            if (eventItem.StartsAtUtc <= now)
            {
                throw new ConflictException("Registration cannot be cancelled after the event has started.");
            }

            try
            {
                registration.Cancel(now);
                _notificationService.AddRegistrationCancelled(
                    eventItem.OrganizerUserId,
                    eventItem.Id,
                    eventItem.Title,
                    now);
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
