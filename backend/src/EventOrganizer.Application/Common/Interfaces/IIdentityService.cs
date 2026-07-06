namespace EventOrganizer.Application.Common.Interfaces
{
    public interface IIdentityService
    {
        Task<AuthUserResult?> FindByEmailAsync(
            string email,
            CancellationToken cancellationToken);

        Task<AuthUserResult> CreateUserAsync(
            string fullName,
            string email,
            string password,
            CancellationToken cancellationToken);

        Task<bool> CheckPasswordAsync(
            Guid userId,
            string password,
            CancellationToken cancellationToken);

        Task<IReadOnlyCollection<string>> GetRolesAsync(
            Guid userId,
            CancellationToken cancellationToken);

        Task AddToRoleAsync(
            Guid userId,
            string role,
            CancellationToken cancellationToken);
    }
}