using EventOrganizer.Application.Commands.RegisterUser;
using EventOrganizer.Application.Common.Constants;
using EventOrganizer.Application.Common.Exceptions;
using EventOrganizer.Application.Common.Interfaces;
using EventOrganizer.Domain.Users;

namespace EventOrganizer.Tests.Application.Auth
{
    public sealed class RegisterUserCommandHandlerTests
    {
        [Fact]
        public async Task Handle_WithValidCommand_CreatesUserAssignsParticipantRoleAndReturnsAuthResponse()
        {
            var identityService = new FakeIdentityService();
            var tokenService = new FakeTokenService();

            var handler = new RegisterUserCommandHandler(identityService, tokenService);

            var command = new RegisterUserCommand(
                "Test User",
                "test.user@example.com",
                "Password1");

            var result = await handler.Handle(command, CancellationToken.None);

            Assert.Equal(command.FullName, result.FullName);
            Assert.Equal(command.Email, result.Email);
            Assert.Equal("access-token", result.AccessToken);
            Assert.Contains(ApplicationRoles.Participant, result.Roles);

            Assert.Single(identityService.CreatedUsers);
            Assert.Equal(command.Email, identityService.CreatedUsers[0].Email);

            Assert.Single(identityService.AssignedRoles);
            Assert.Equal(result.UserId, identityService.AssignedRoles[0].UserId);
            Assert.Equal(ApplicationRoles.Participant, identityService.AssignedRoles[0].Role);
        }

        [Fact]
        public async Task Handle_WhenEmailAlreadyExists_ThrowsConflictException()
        {
            var existingUser = new AuthUserResult(
                Guid.NewGuid(),
                "Existing User",
                "test.user@example.com",
                UserStatus.Active);

            var identityService = new FakeIdentityService(existingUser);
            var tokenService = new FakeTokenService();

            var handler = new RegisterUserCommandHandler(identityService, tokenService);

            var command = new RegisterUserCommand(
                "Test User",
                "test.user@example.com",
                "Password1");

            await Assert.ThrowsAsync<ConflictException>(() =>
                handler.Handle(command, CancellationToken.None));

            Assert.Empty(identityService.CreatedUsers);
            Assert.Empty(identityService.AssignedRoles);
        }

        private sealed class FakeIdentityService : IIdentityService
        {
            private readonly AuthUserResult? _existingUser;
            private readonly Dictionary<Guid, AuthUserResult> _users = new();
            private readonly Dictionary<Guid, List<string>> _roles = new();

            public FakeIdentityService(AuthUserResult? existingUser = null)
            {
                _existingUser = existingUser;

                if (existingUser is not null)
                {
                    _users[existingUser.UserId] = existingUser;
                }
            }

            public List<AuthUserResult> CreatedUsers { get; } = [];

            public List<(Guid UserId, string Role)> AssignedRoles { get; } = [];

            public Task<AuthUserResult?> FindByEmailAsync(
                string email,
                CancellationToken cancellationToken)
            {
                if (_existingUser is not null &&
                    string.Equals(_existingUser.Email, email, StringComparison.OrdinalIgnoreCase))
                {
                    return Task.FromResult<AuthUserResult?>(_existingUser);
                }

                return Task.FromResult<AuthUserResult?>(null);
            }

            public Task<AuthUserResult> CreateUserAsync(
                string fullName,
                string email,
                string password,
                CancellationToken cancellationToken)
            {
                var user = new AuthUserResult(
                    Guid.NewGuid(),
                    fullName,
                    email,
                    UserStatus.Active);

                _users[user.UserId] = user;
                CreatedUsers.Add(user);

                return Task.FromResult(user);
            }

            public Task<bool> CheckPasswordAsync(
                Guid userId,
                string password,
                CancellationToken cancellationToken)
            {
                return Task.FromResult(_users.ContainsKey(userId));
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
                if (!_roles.ContainsKey(userId))
                {
                    _roles[userId] = [];
                }

                _roles[userId].Add(role);
                AssignedRoles.Add((userId, role));

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
    }
}
