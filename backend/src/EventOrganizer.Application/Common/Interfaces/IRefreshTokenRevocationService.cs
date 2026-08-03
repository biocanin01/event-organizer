namespace EventOrganizer.Application.Common.Interfaces
{
    public interface IRefreshTokenRevocationService
    {
        Task RevokeAsync(
            string tokenHash,
            string? ipAddress,
            CancellationToken cancellationToken);

        Task RevokeAllForUserAsync(
            Guid userId,
            string? ipAddress,
            CancellationToken cancellationToken);
    }
}
