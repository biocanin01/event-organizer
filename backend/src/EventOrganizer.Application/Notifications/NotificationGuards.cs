using EventOrganizer.Application.Common.Exceptions;
using EventOrganizer.Application.Common.Interfaces;

namespace EventOrganizer.Application.Notifications
{
    internal static class NotificationGuards
    {
        public static Guid RequireAuthenticatedUser(ICurrentUserService currentUserService)
        {
            return currentUserService.IsAuthenticated && currentUserService.UserId.HasValue
                ? currentUserService.UserId.Value
                : throw new UnauthorizedException("An authenticated user is required.");
        }
    }
}
