using EventOrganizer.Application.Common.Constants;
using EventOrganizer.Application.Common.Exceptions;
using EventOrganizer.Application.Common.Interfaces;
using EventOrganizer.Domain.Events;
using EventOrganizer.Domain.Resources;

namespace EventOrganizer.Application.Common.Authorization
{
    public sealed class ResourceReservationAuthorizationService
    {
        private readonly ICurrentUserService _currentUserService;

        public ResourceReservationAuthorizationService(ICurrentUserService currentUserService)
        {
            _currentUserService = currentUserService;
        }

        public void EnsureCanCancel(ResourceReservation reservation, Event eventItem)
        {
            if (!_currentUserService.IsAuthenticated || _currentUserService.UserId is null)
            {
                throw new UnauthorizedException(
                    "An authenticated user is required to cancel resource reservations.");
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

            throw new ForbiddenException(
                "The current user is not allowed to cancel this resource reservation.");
        }
    }
}
