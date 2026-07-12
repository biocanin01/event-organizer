using EventOrganizer.Application.Common.Interfaces;
using EventOrganizer.Infrastructure.Identity;
using EventOrganizer.Infrastructure.Persistance;

namespace EventOrganizer.Infrastructure.Authentication
{
    public sealed class RefreshTokenStore : IRefreshTokenStore
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
    }
}
