using EventOrganizer.Domain.Users;

namespace EventOrganizer.Application.Common.Interfaces
{
    public interface IUserManagementService
    {
        Task<IReadOnlyList<UserSummaryResult>> ListUsersAsync(
            UserListFilter filter,
            CancellationToken cancellationToken);

        Task<UserSummaryResult?> FindUserSummaryByIdAsync(
            Guid userId,
            CancellationToken cancellationToken);

        Task<IReadOnlyDictionary<Guid, UserSummaryResult>> FindUserSummariesByIdsAsync(
            IReadOnlyCollection<Guid> userIds,
            CancellationToken cancellationToken);

        Task UpdateUserStatusAsync(
            Guid userId,
            UserStatus status,
            CancellationToken cancellationToken);
    }
}
