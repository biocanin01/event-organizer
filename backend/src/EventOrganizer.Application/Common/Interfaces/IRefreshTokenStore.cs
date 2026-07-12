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
    }
}
