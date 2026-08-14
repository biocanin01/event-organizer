using EventOrganizer.Application.Common.Authorization;
using EventOrganizer.Application.Common.Constants;
using EventOrganizer.Application.Common.Exceptions;
using EventOrganizer.Application.Common.Interfaces;
using EventOrganizer.Domain.Events;
using EventOrganizer.Domain.Registrations;

namespace EventOrganizer.Application.Registrations
{
    internal static class RegistrationGuards
    {
        public static Guid RequireParticipant(ICurrentUserService currentUserService)
        {
            if (!currentUserService.IsAuthenticated || currentUserService.UserId is null)
            {
                throw new UnauthorizedException("An authenticated participant is required.");
            }

            if (!currentUserService.IsInRole(ApplicationRoles.Participant))
            {
                throw new ForbiddenException("Only participants can register for events.");
            }

            return currentUserService.UserId.Value;
        }

        public static Guid RequireAuthenticatedUser(ICurrentUserService currentUserService)
        {
            return currentUserService.IsAuthenticated && currentUserService.UserId.HasValue
                ? currentUserService.UserId.Value
                : throw new UnauthorizedException("An authenticated user is required.");
        }

        public static void EnsureOwner(Registration registration, Guid userId)
        {
            if (registration.ParticipantUserId != userId)
            {
                throw new ForbiddenException("The current user cannot change this registration.");
            }
        }

        public static void EnsureExpectedVersion(Registration registration, int version)
        {
            if (registration.Version != version)
            {
                throw new ConflictException("The registration has changed. Refresh it and try again.");
            }
        }

        public static Guid RequireEventManager(
            Event eventItem,
            ICurrentUserService currentUserService,
            EventAuthorizationService eventAuthorizationService)
        {
            eventAuthorizationService.EnsureCanManage(eventItem);
            return RequireAuthenticatedUser(currentUserService);
        }
    }
}
