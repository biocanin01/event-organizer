using EventOrganizer.Application.Common.Interfaces;
using EventOrganizer.Infrastructure.Identity;
using EventOrganizer.Infrastructure.Persistance;
using Microsoft.EntityFrameworkCore;

namespace EventOrganizer.Infrastructure.Authentication
{
    public sealed class RefreshTokenStore
        : IRefreshTokenStore, IRefreshTokenRevocationService
    {
        private readonly AppDbContext _dbContext;

        public RefreshTokenStore(AppDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task StoreAsync(
            Guid userId,
            string tokenHash,
            DateTime expiresAtUtc,
            string? ipAddress,
            CancellationToken cancellationToken)
        {
            var refreshToken = new RefreshToken
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                TokenHash = tokenHash,
                ExpiresAtUtc = expiresAtUtc,
                CreatedAtUtc = DateTime.UtcNow,
                CreatedByIpAddress = ipAddress,
            };

            _dbContext.RefreshTokens.Add(refreshToken);

            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        public async Task<StoredRefreshTokenResult?> FindByTokenHashAsync(
            string tokenHash,
            CancellationToken cancellationToken)
        {
            return await _dbContext.RefreshTokens
                .AsNoTracking()
                .Where(refreshToken => refreshToken.TokenHash == tokenHash)
                .Select(refreshToken => new StoredRefreshTokenResult(
                    refreshToken.Id,
                    refreshToken.UserId,
                    refreshToken.TokenHash,
                    refreshToken.ExpiresAtUtc,
                    refreshToken.RevokedAtUtc))
                .FirstOrDefaultAsync(cancellationToken);
        }

        public async Task RotateAsync(
            Guid refreshTokenId,
            Guid userId,
            string newTokenHash,
            DateTime newExpiresAtUtc,
            string? ipAddress,
            CancellationToken cancellationToken)
        {
            var existingRefreshToken = await _dbContext.RefreshTokens
                .FirstOrDefaultAsync(
                    refreshToken => refreshToken.Id == refreshTokenId,
                    cancellationToken);

            if (existingRefreshToken is null)
            {
                throw new InvalidOperationException(
                    $"Refresh token with id '{refreshTokenId}' was not found.");
            }

            existingRefreshToken.RevokedAtUtc = DateTime.UtcNow;
            existingRefreshToken.RevokedByIpAddress = ipAddress;
            existingRefreshToken.ReplacedByTokenHash = newTokenHash;

            _dbContext.RefreshTokens.Add(new Infrastructure.Identity.RefreshToken
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                TokenHash = newTokenHash,
                ExpiresAtUtc = newExpiresAtUtc,
                CreatedAtUtc = DateTime.UtcNow,
                CreatedByIpAddress = ipAddress,
            });

            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        public async Task RevokeAsync(
            string tokenHash,
            string? ipAddress,
            CancellationToken cancellationToken)
        {
            var refreshToken = await _dbContext.RefreshTokens
                .FirstOrDefaultAsync(
                    token => token.TokenHash == tokenHash,
                    cancellationToken);

            if (refreshToken is null || refreshToken.IsRevoked)
            {
                return;
            }

            refreshToken.RevokedAtUtc = DateTime.UtcNow;
            refreshToken.RevokedByIpAddress = ipAddress;

            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        public async Task RevokeAllForUserAsync(
            Guid userId,
            string? ipAddress,
            CancellationToken cancellationToken)
        {
            var activeRefreshTokens = await _dbContext.RefreshTokens
                .Where(token =>
                    token.UserId == userId
                    && token.RevokedAtUtc == null)
                .ToListAsync(cancellationToken);

            if (activeRefreshTokens.Count == 0)
            {
                return;
            }

            var revokedAtUtc = DateTime.UtcNow;

            foreach (var refreshToken in activeRefreshTokens)
            {
                refreshToken.RevokedAtUtc = revokedAtUtc;
                refreshToken.RevokedByIpAddress = ipAddress;
            }

            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}
