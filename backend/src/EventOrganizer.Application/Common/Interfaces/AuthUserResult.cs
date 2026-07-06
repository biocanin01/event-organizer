using EventOrganizer.Domain.Users;

namespace EventOrganizer.Application.Common.Interfaces
{
    public sealed record AuthUserResult(
        Guid UserId,
        string FullName,
        string Email,
        UserStatus Status);
}