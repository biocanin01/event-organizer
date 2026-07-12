using EventOrganizer.Application.Commands.LoginUser;
using EventOrganizer.Application.Common.Constants;
using EventOrganizer.Application.Common.Exceptions;
using EventOrganizer.Application.Common.Interfaces;
using EventOrganizer.Domain.Users;

namespace EventOrganizer.Tests.Application.Auth
{
    public sealed class LoginUserCommandHandlerTests
    {
        [Fact]
        public async Task Handle_WithValidCredentials_ReturnsAuthResponse()
        {
            var user = new AuthUserResult(
                Guid.NewGuid(),
                "Test User",
                "test.user@example.com",
                UserStatus.Active);

            var identityService = new FakeIdentityService(user, "Password1");
            identityService.SetRoles(user.UserId, [ApplicationRoles.Participant]);

            var clientContextService = new FakeClientContextService();
            var refreshTokenService = new FakeRefreshTokenService();
            var refreshTokenStore = new FakeRefreshTokenStore();
            var tokenService = new FakeTokenService();

            var handler = new LoginUserCommandHandler(
                identityService,
                clientContextService,
                refreshTokenService,
                refreshTokenStore,
                tokenService);

            var command = new LoginUserCommand(
                "test.user@example.com",
                "Password1");

            var result = await handler.Handle(command, CancellationToken.None);

            Assert.Equal(user.UserId, result.UserId);
            Assert.Equal(user.FullName, result.FullName);
            Assert.Equal(user.Email, result.Email);
            Assert.Equal("access-token", result.AccessToken);
            Assert.Equal("refresh-token", result.RefreshToken);
            Assert.Equal(
                new DateTime(2026, 7, 18, 12, 0, 0, DateTimeKind.Utc),
                result.RefreshTokenExpiresAtUtc);
            Assert.Contains(ApplicationRoles.Participant, result.Roles);

            Assert.Single(refreshTokenStore.StoredRefreshTokens);
            Assert.Equal(user.UserId, refreshTokenStore.StoredRefreshTokens[0].UserId);
            Assert.Equal("refresh-token-hash", refreshTokenStore.StoredRefreshTokens[0].TokenHash);
            Assert.Equal("127.0.0.1", refreshTokenStore.StoredRefreshTokens[0].IpAddress);
        }

        [Fact]
        public async Task Handle_WhenEmailDoesNotExist_ThrowsUnauthorizedException()
        {
            var identityService = new FakeIdentityService();
            var clientContextService = new FakeClientContextService();
            var refreshTokenService = new FakeRefreshTokenService();
            var refreshTokenStore = new FakeRefreshTokenStore();
            var tokenService = new FakeTokenService();

            var handler = new LoginUserCommandHandler(
                identityService,
                clientContextService,
                refreshTokenService,
                refreshTokenStore,
                tokenService);

            var command = new LoginUserCommand(
                "missing.user@example.com",
                "Password1");

            await Assert.ThrowsAsync<UnauthorizedException>(() =>
                handler.Handle(command, CancellationToken.None));

            Assert.Empty(refreshTokenStore.StoredRefreshTokens);
        }

        [Fact]
        public async Task Handle_WhenPasswordIsInvalid_ThrowsUnauthorizedException()
        {
            var user = new AuthUserResult(
                Guid.NewGuid(),
                "Test User",
                "test.user@example.com",
                UserStatus.Active);

            var identityService = new FakeIdentityService(user, "Password1");
            var clientContextService = new FakeClientContextService();
            var refreshTokenService = new FakeRefreshTokenService();
            var refreshTokenStore = new FakeRefreshTokenStore();
            var tokenService = new FakeTokenService();

            var handler = new LoginUserCommandHandler(
                identityService,
                clientContextService,
                refreshTokenService,
                refreshTokenStore,
                tokenService);

            var command = new LoginUserCommand(
                "test.user@example.com",
                "WrongPassword1");

            await Assert.ThrowsAsync<UnauthorizedException>(() =>
                handler.Handle(command, CancellationToken.None));

            Assert.Empty(refreshTokenStore.StoredRefreshTokens);
        }

        [Theory]
        [InlineData(UserStatus.PendingVerification)]
        [InlineData(UserStatus.Suspended)]
        [InlineData(UserStatus.Deleted)]
        public async Task Handle_WhenUserIsNotActive_ThrowsUnauthorizedException(UserStatus status)
        {
            var user = new AuthUserResult(
                Guid.NewGuid(),
                "Test User",
                "test.user@example.com",
                status);

            var identityService = new FakeIdentityService(user, "Password1");
            var clientContextService = new FakeClientContextService();
            var refreshTokenService = new FakeRefreshTokenService();
            var refreshTokenStore = new FakeRefreshTokenStore();
            var tokenService = new FakeTokenService();

            var handler = new LoginUserCommandHandler(
                identityService,
                clientContextService,
                refreshTokenService,
                refreshTokenStore,
                tokenService);

            var command = new LoginUserCommand(
                "test.user@example.com",
                "Password1");

            await Assert.ThrowsAsync<UnauthorizedException>(() =>
                handler.Handle(command, CancellationToken.None));

            Assert.Empty(refreshTokenStore.StoredRefreshTokens);
        }

        private sealed class FakeIdentityService : IIdentityService
        {
            private readonly string? _password;
            private readonly Dictionary<Guid, List<string>> _roles = new();

            public FakeIdentityService(
                AuthUserResult? user = null,
                string? password = null)
            {
                User = user;
                _password = password;
            }

            private AuthUserResult? User { get; }

            public Task<AuthUserResult?> FindByEmailAsync(
                string email,
                CancellationToken cancellationToken)
            {
                if (User is not null &&
                    string.Equals(User.Email, email, StringComparison.OrdinalIgnoreCase))
                {
                    return Task.FromResult<AuthUserResult?>(User);
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

            public Task<bool> CheckPasswordAsync(
                Guid userId,
                string password,
                CancellationToken cancellationToken)
            {
                return Task.FromResult(
                    User is not null &&
                    User.UserId == userId &&
                    _password == password);
            }

            public Task<IReadOnlyCollection<string>> GetRolesAsync(
                Guid userId,
                CancellationToken cancellationToken)
            {
                if (!_roles.TryGetValue(userId, out var roles))
                {
                    return Task.FromResult<IReadOnlyCollection<string>>(Array.Empty<string>());
                }

                return Task.FromResult<IReadOnlyCollection<string>>(roles.ToArray());
            }

            public Task AddToRoleAsync(
                Guid userId,
                string role,
                CancellationToken cancellationToken)
            {
                throw new NotSupportedException();
            }

            public void SetRoles(Guid userId, IReadOnlyCollection<string> roles)
            {
                _roles[userId] = roles.ToList();
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

        private sealed class FakeRefreshTokenService : IRefreshTokenService
        {
            public RefreshTokenResult CreateRefreshToken()
            {
                return new RefreshTokenResult(
                    "refresh-token",
                    "refresh-token-hash",
                    new DateTime(2026, 7, 18, 12, 0, 0, DateTimeKind.Utc));
            }

            public string HashToken(string token)
            {
                return $"{token}-hash";
            }
        }

        private sealed class FakeRefreshTokenStore : IRefreshTokenStore
        {
            public List<(Guid UserId, string TokenHash, DateTime ExpiresAtUtc, string? IpAddress)> StoredRefreshTokens { get; } = [];

            public Task StoreAsync(
                Guid userId,
                string tokenHash,
                DateTime expiresAtUtc,
                string? ipAddress,
                CancellationToken cancellationToken)
            {
                StoredRefreshTokens.Add((userId, tokenHash, expiresAtUtc, ipAddress));

                return Task.CompletedTask;
            }
        }

        private sealed class FakeClientContextService : IClientContextService
        {
            public string? IpAddress => "127.0.0.1";
        }
    }
}
