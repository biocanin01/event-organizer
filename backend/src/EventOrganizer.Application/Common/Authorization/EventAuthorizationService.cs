using EventOrganizer.Application.Common.Constants;
using EventOrganizer.Application.Common.Exceptions;
using EventOrganizer.Application.Common.Interfaces;
using EventOrganizer.Domain.Events;

namespace EventOrganizer.Application.Common.Authorization
{
    public sealed class EventAuthorizationService
    {
        private readonly ICurrentUserService _currentUserService;

        public EventAuthorizationService(ICurrentUserService currentUserService)
        {
            _currentUserService = currentUserService;
        }

        public void EnsureCanManage(Event eventItem)
        {
            if (!_currentUserService.IsAuthenticated || _currentUserService.UserId is null)
            {
                throw new UnauthorizedException("An authenticated user is required to manage events.");
            }

            if (_currentUserService.IsInRole(ApplicationRoles.Admin))
            {
                return;
            }

            if (_currentUserService.IsInRole(ApplicationRoles.Organizer) &&
                eventItem.OrganizerUserId == _currentUserService.UserId.Value)
            {
                return;
            }

            throw new ForbiddenException("The current user is not allowed to manage this event.");
        }
    }
}
