namespace EventOrganizer.Application.Common.Interfaces
{
    public interface IRefreshTokenStore
    {
        Task StoreAsync(
            Guid userId,
            string tokenHash,
            DateTime expiresAtUtc,
            string? ipAddress,
            CancellationToken cancellationToken);

        Task<StoredRefreshTokenResult?> FindByTokenHashAsync(
            string tokenHash,
            CancellationToken cancellationToken);

        Task RotateAsync(
            Guid refreshTokenId,
            Guid userId,
            string newTokenHash,
            DateTime newExpiresAtUtc,
            string? ipAddress,
            CancellationToken cancellationToken);
    }
}
