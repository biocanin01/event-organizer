using EventOrganizer.Application.Common.Constants;
using EventOrganizer.Application.Common.Exceptions;
using EventOrganizer.Application.Common.Interfaces;
using EventOrganizer.Domain.Users;

namespace EventOrganizer.Application.Common.Users
{
    public static class UserStatusManagementRules
    {
        public static void EnsureCanChangeStatus(
            Guid currentUserId,
            UserSummaryResult targetUser)
        {
            if (targetUser.UserId == currentUserId)
            {
                throw new ConflictException("Admin users cannot change their own account status.");
            }

            if (targetUser.Roles.Contains(ApplicationRoles.Admin))
            {
                throw new ConflictException("Admin account status cannot be managed through this workflow.");
            }

            if (targetUser.Status == UserStatus.Deleted)
            {
                throw new ConflictException("Deleted users cannot be managed through this workflow.");
            }
        }
    }
}
