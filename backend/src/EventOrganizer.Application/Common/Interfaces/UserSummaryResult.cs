using EventOrganizer.Domain.Users;

namespace EventOrganizer.Application.Common.Interfaces
{
    public sealed record UserSummaryResult(
        Guid UserId,
        string FullName,
        string Email,
        UserStatus Status,
        DateTime CreatedAtUtc,
        DateTime? VerifiedAtUtc,
        IReadOnlyCollection<string> Roles);
}
