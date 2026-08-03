using EventOrganizer.Application.Common.Interfaces;
using EventOrganizer.Application.Responses;

namespace EventOrganizer.Application.Common.Mapping
{
    public static class UserResponseMapper
    {
        public static UserResponse ToResponse(UserSummaryResult user)
        {
            return new UserResponse(
                user.UserId,
                user.FullName,
                user.Email,
                user.Status.ToString(),
                user.CreatedAtUtc,
                user.VerifiedAtUtc,
                user.Roles);
        }
    }
}
