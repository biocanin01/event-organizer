using EventOrganizer.Application.Common.Constants;
using EventOrganizer.Application.Common.Exceptions;
using EventOrganizer.Application.Common.Interfaces;

namespace EventOrganizer.Application.Bookings
{
    internal static class EventBookingAdminGuard
    {
        public static Guid RequireAdminUserId(ICurrentUserService currentUserService)
        {
            if (!currentUserService.IsAuthenticated || currentUserService.UserId is null)
            {
                throw new UnauthorizedException("An authenticated admin user is required.");
            }

            if (!currentUserService.IsInRole(ApplicationRoles.Admin))
            {
                throw new ForbiddenException("Only admins can manage booking approvals.");
            }

            return currentUserService.UserId.Value;
        }
    }
}
