using EventOrganizer.Application.Commands.RefreshToken;
using EventOrganizer.Application.Common.Constants;
using EventOrganizer.Application.Common.Exceptions;
using EventOrganizer.Application.Common.Interfaces;
using EventOrganizer.Domain.Users;

namespace EventOrganizer.Tests.Application.Auth
{
    public sealed class RefreshTokenCommandHandlerTests
    {
        [Fact]
        public async Task Handle_WithValidRefreshToken_ReturnsNewAuthResponseAndRotatesToken()
        {
            var userId = Guid.NewGuid();
            var refreshTokenId = Guid.NewGuid();

            var user = new AuthUserResult(
                userId,
                "Test User",
                "test.user@example.com",
                UserStatus.Active);

            var storedRefreshToken = new StoredRefreshTokenResult(
                refreshTokenId,
                userId,
                "old-refresh-token-hash",
                DateTime.UtcNow.AddDays(1),
                null);

            var identityService = new FakeIdentityService(user);
            identityService.SetRoles(userId, [ApplicationRoles.Participant]);

            var refreshTokenStore = new FakeRefreshTokenStore(storedRefreshToken);

            var handler = new RefreshTokenCommandHandler(
                identityService,
                new FakeClientContextService(),
                new FakeRefreshTokenService(),
                refreshTokenStore,
                new FakeTokenService());

            var result = await handler.Handle(
                new RefreshTokenCommand("old-refresh-token"),
                CancellationToken.None);

            Assert.Equal(user.UserId, result.UserId);
            Assert.Equal(user.FullName, result.FullName);
            Assert.Equal(user.Email, result.Email);
            Assert.Equal("access-token", result.AccessToken);
            Assert.Equal("new-refresh-token", result.RefreshToken);
            Assert.Equal(
                new DateTime(2026, 7, 18, 12, 0, 0, DateTimeKind.Utc),
                result.RefreshTokenExpiresAtUtc);
            Assert.Contains(ApplicationRoles.Participant, result.Roles);

            Assert.Single(refreshTokenStore.RotatedRefreshTokens);
            Assert.Equal(refreshTokenId, refreshTokenStore.RotatedRefreshTokens[0].RefreshTokenId);
            Assert.Equal(userId, refreshTokenStore.RotatedRefreshTokens[0].UserId);
            Assert.Equal("new-refresh-token-hash", refreshTokenStore.RotatedRefreshTokens[0].NewTokenHash);
            Assert.Equal("127.0.0.1", refreshTokenStore.RotatedRefreshTokens[0].IpAddress);
        }

        [Fact]
        public async Task Handle_WhenRefreshTokenDoesNotExist_ThrowsUnauthorizedException()
        {
            var handler = new RefreshTokenCommandHandler(
                new FakeIdentityService(),
                new FakeClientContextService(),
                new FakeRefreshTokenService(),
                new FakeRefreshTokenStore(),
                new FakeTokenService());

            await Assert.ThrowsAsync<UnauthorizedException>(() =>
                handler.Handle(new RefreshTokenCommand("missing-refresh-token"), CancellationToken.None));
        }

        [Fact]
        public async Task Handle_WhenRefreshTokenIsExpired_ThrowsUnauthorizedException()
        {
            var storedRefreshToken = new StoredRefreshTokenResult(
                Guid.NewGuid(),
                Guid.NewGuid(),
                "old-refresh-token-hash",
                DateTime.UtcNow.AddMinutes(-1),
                null);

            var handler = new RefreshTokenCommandHandler(
                new FakeIdentityService(),
                new FakeClientContextService(),
                new FakeRefreshTokenService(),
                new FakeRefreshTokenStore(storedRefreshToken),
                new FakeTokenService());

            await Assert.ThrowsAsync<UnauthorizedException>(() =>
                handler.Handle(new RefreshTokenCommand("old-refresh-token"), CancellationToken.None));
        }

        [Fact]
        public async Task Handle_WhenRefreshTokenIsRevoked_ThrowsUnauthorizedException()
        {
            var storedRefreshToken = new StoredRefreshTokenResult(
                Guid.NewGuid(),
                Guid.NewGuid(),
                "old-refresh-token-hash",
                DateTime.UtcNow.AddDays(1),
                DateTime.UtcNow);

            var handler = new RefreshTokenCommandHandler(
                new FakeIdentityService(),
                new FakeClientContextService(),
                new FakeRefreshTokenService(),
                new FakeRefreshTokenStore(storedRefreshToken),
                new FakeTokenService());

            await Assert.ThrowsAsync<UnauthorizedException>(() =>
                handler.Handle(new RefreshTokenCommand("old-refresh-token"), CancellationToken.None));
        }

        [Theory]
        [InlineData(UserStatus.PendingVerification)]
        [InlineData(UserStatus.Suspended)]
        [InlineData(UserStatus.Deleted)]
        public async Task Handle_WhenUserIsNotActive_ThrowsUnauthorizedException(UserStatus status)
        {
            var userId = Guid.NewGuid();

            var user = new AuthUserResult(
                userId,
                "Test User",
                "test.user@example.com",
                status);

            var storedRefreshToken = new StoredRefreshTokenResult(
                Guid.NewGuid(),
                userId,
                "old-refresh-token-hash",
                DateTime.UtcNow.AddDays(1),
                null);

            var handler = new RefreshTokenCommandHandler(
                new FakeIdentityService(user),
                new FakeClientContextService(),
                new FakeRefreshTokenService(),
                new FakeRefreshTokenStore(storedRefreshToken),
                new FakeTokenService());

            await Assert.ThrowsAsync<UnauthorizedException>(() =>
                handler.Handle(new RefreshTokenCommand("old-refresh-token"), CancellationToken.None));
        }

        [Fact]
        public async Task Handle_WhenUserDoesNotExist_ThrowsUnauthorizedException()
        {
            var storedRefreshToken = new StoredRefreshTokenResult(
                Guid.NewGuid(),
                Guid.NewGuid(),
                "old-refresh-token-hash",
                DateTime.UtcNow.AddDays(1),
                null);

            var handler = new RefreshTokenCommandHandler(
                new FakeIdentityService(),
                new FakeClientContextService(),
                new FakeRefreshTokenService(),
                new FakeRefreshTokenStore(storedRefreshToken),
                new FakeTokenService());

            await Assert.ThrowsAsync<UnauthorizedException>(() =>
                handler.Handle(new RefreshTokenCommand("old-refresh-token"), CancellationToken.None));
        }

        private sealed class FakeIdentityService : IIdentityService
        {
            private readonly AuthUserResult? _user;
            private readonly Dictionary<Guid, List<string>> _roles = new();

            public FakeIdentityService(AuthUserResult? user = null)
            {
                _user = user;
            }

            public Task<AuthUserResult?> FindByEmailAsync(string email, CancellationToken cancellationToken)
            {
                throw new NotSupportedException();
            }

            public Task<AuthUserResult?> FindByIdAsync(Guid userId, CancellationToken cancellationToken)
            {
                if (_user is not null && _user.UserId == userId)
                {
                    return Task.FromResult<AuthUserResult?>(_user);
                }

                return Task.FromResult<AuthUserResult?>(null);
            }

            public Task<AuthUserResult> CreateUserAsync(
                string fullName,
                string email,
                string password,
                CancellationToken cancellationToken)
            {
                throw new NotSupportedException();
            }

            public Task<bool> CheckPasswordAsync(Guid userId, string password, CancellationToken cancellationToken)
            {
                throw new NotSupportedException();
            }

            public Task<IReadOnlyCollection<string>> GetRolesAsync(Guid userId, CancellationToken cancellationToken)
            {
                if (!_roles.TryGetValue(userId, out var roles))
                {
                    return Task.FromResult<IReadOnlyCollection<string>>(Array.Empty<string>());
                }

                return Task.FromResult<IReadOnlyCollection<string>>(roles.ToArray());
            }

            public Task AddToRoleAsync(Guid userId, string role, CancellationToken cancellationToken)
            {
                throw new NotSupportedException();
            }

            public void SetRoles(Guid userId, IReadOnlyCollection<string> roles)
            {
                _roles[userId] = roles.ToList();
            }
        }

        private sealed class FakeRefreshTokenService : IRefreshTokenService
        {
            public RefreshTokenResult CreateRefreshToken()
            {
                return new RefreshTokenResult(
                    "new-refresh-token",
                    "new-refresh-token-hash",
                    new DateTime(2026, 7, 18, 12, 0, 0, DateTimeKind.Utc));
            }

            public string HashToken(string token)
            {
                return $"{token}-hash";
            }
        }

        private sealed class FakeRefreshTokenStore : IRefreshTokenStore
        {
            private readonly StoredRefreshTokenResult? _storedRefreshToken;

            public FakeRefreshTokenStore(StoredRefreshTokenResult? storedRefreshToken = null)
            {
                _storedRefreshToken = storedRefreshToken;
            }

            public List<(Guid RefreshTokenId, Guid UserId, string NewTokenHash, DateTime NewExpiresAtUtc, string? IpAddress)> RotatedRefreshTokens { get; } = [];

            public Task StoreAsync(
                Guid userId,
                string tokenHash,
                DateTime expiresAtUtc,
                string? ipAddress,
                CancellationToken cancellationToken)
            {
                throw new NotSupportedException();
            }

            public Task<StoredRefreshTokenResult?> FindByTokenHashAsync(
                string tokenHash,
                CancellationToken cancellationToken)
            {
                if (_storedRefreshToken is not null && _storedRefreshToken.TokenHash == tokenHash)
                {
                    return Task.FromResult<StoredRefreshTokenResult?>(_storedRefreshToken);
                }

                return Task.FromResult<StoredRefreshTokenResult?>(null);
            }

            public Task RotateAsync(
                Guid refreshTokenId,
                Guid userId,
                string newTokenHash,
                DateTime newExpiresAtUtc,
                string? ipAddress,
                CancellationToken cancellationToken)
            {
                RotatedRefreshTokens.Add((
                    refreshTokenId,
                    userId,
                    newTokenHash,
                    newExpiresAtUtc,
                    ipAddress));

                return Task.CompletedTask;
            }
        }

        private sealed class FakeTokenService : ITokenService
        {
            public AccessTokenResult CreateAccessToken(
                Guid userId,
                string email,
                string fullName,
                IReadOnlyCollection<string> roles)
            {
                return new AccessTokenResult(
                    "access-token",
                    new DateTime(2026, 7, 11, 12, 0, 0, DateTimeKind.Utc));
            }
        }

        private sealed class FakeClientContextService : IClientContextService
        {
            public string? IpAddress => "127.0.0.1";
        }
    }
}