using EventOrganizer.Application.Commands.ReactivateUser;
using EventOrganizer.Application.Commands.SuspendUser;
using EventOrganizer.Application.Common.Constants;
using EventOrganizer.Application.Common.Exceptions;
using EventOrganizer.Application.Common.Interfaces;
using EventOrganizer.Application.Queries.GetUserById;
using EventOrganizer.Application.Queries.ListUsers;
using EventOrganizer.Domain.Users;

namespace EventOrganizer.Tests.Application.Users
{
    public sealed class AdminUserManagementTests : ApplicationTestBase
    {
        [Fact]
        public async Task SuspendUser_WithActiveParticipant_SuspendsUser()
        {
            var adminUserId = Guid.NewGuid();
            var participantUserId = Guid.NewGuid();
            var userManagementService = new FakeUserManagementService(
                CreateUser(adminUserId, "Admin", ApplicationRoles.Admin),
                CreateUser(participantUserId, "Participant", ApplicationRoles.Participant));
            var refreshTokenRevocationService = new FakeRefreshTokenRevocationService();
            var handler = new SuspendUserCommandHandler(
                new TestCurrentUserService(adminUserId, ApplicationRoles.Admin),
                new TestClientContextService(),
                refreshTokenRevocationService,
                userManagementService);

            await handler.Handle(
                new SuspendUserCommand(participantUserId),
                CancellationToken.None);

            Assert.Equal(UserStatus.Suspended, userManagementService.Users[participantUserId].Status);
            Assert.Contains(participantUserId, refreshTokenRevocationService.RevokedUserIds);
        }

        [Fact]
        public async Task SuspendUser_WhenTargetIsCurrentAdmin_ThrowsConflictException()
        {
            var adminUserId = Guid.NewGuid();
            var userManagementService = new FakeUserManagementService(
                CreateUser(adminUserId, "Admin", ApplicationRoles.Admin));
            var handler = new SuspendUserCommandHandler(
                new TestCurrentUserService(adminUserId, ApplicationRoles.Admin),
                new TestClientContextService(),
                new FakeRefreshTokenRevocationService(),
                userManagementService);

            await Assert.ThrowsAsync<ConflictException>(() =>
                handler.Handle(
                    new SuspendUserCommand(adminUserId),
                    CancellationToken.None));
        }

        [Fact]
        public async Task SuspendUser_WhenTargetIsAdmin_ThrowsConflictException()
        {
            var currentAdminUserId = Guid.NewGuid();
            var targetAdminUserId = Guid.NewGuid();
            var userManagementService = new FakeUserManagementService(
                CreateUser(currentAdminUserId, "Current Admin", ApplicationRoles.Admin),
                CreateUser(targetAdminUserId, "Target Admin", ApplicationRoles.Admin));
            var handler = new SuspendUserCommandHandler(
                new TestCurrentUserService(currentAdminUserId, ApplicationRoles.Admin),
                new TestClientContextService(),
                new FakeRefreshTokenRevocationService(),
                userManagementService);

            await Assert.ThrowsAsync<ConflictException>(() =>
                handler.Handle(
                    new SuspendUserCommand(targetAdminUserId),
                    CancellationToken.None));
        }

        [Fact]
        public async Task ReactivateUser_WithSuspendedUser_ActivatesUser()
        {
            var adminUserId = Guid.NewGuid();
            var participantUserId = Guid.NewGuid();
            var suspendedUser = CreateUser(participantUserId, "Participant", ApplicationRoles.Participant);
            suspendedUser = suspendedUser with { Status = UserStatus.Suspended };
            var userManagementService = new FakeUserManagementService(
                CreateUser(adminUserId, "Admin", ApplicationRoles.Admin),
                suspendedUser);
            var handler = new ReactivateUserCommandHandler(
                new TestCurrentUserService(adminUserId, ApplicationRoles.Admin),
                userManagementService);

            await handler.Handle(
                new ReactivateUserCommand(participantUserId),
                CancellationToken.None);

            Assert.Equal(UserStatus.Active, userManagementService.Users[participantUserId].Status);
        }

        [Fact]
        public async Task ListUsers_WithRoleFilter_ReturnsMatchingUsers()
        {
            var organizerUserId = Guid.NewGuid();
            var userManagementService = new FakeUserManagementService(
                CreateUser(Guid.NewGuid(), "Participant", ApplicationRoles.Participant),
                CreateUser(organizerUserId, "Organizer", ApplicationRoles.Participant, ApplicationRoles.Organizer));
            var handler = new ListUsersQueryHandler(userManagementService);

            var result = await handler.Handle(
                new ListUsersQuery(null, null, ApplicationRoles.Organizer),
                CancellationToken.None);

            Assert.Single(result);
            Assert.Equal(organizerUserId, result[0].Id);
            Assert.Contains(ApplicationRoles.Organizer, result[0].Roles);
        }

        [Fact]
        public async Task GetUserById_ReturnsUserDetailsAndCreatedEventCount()
        {
            var organizerUserId = await CreateOrganizerUserAsync("organizer@example.com");
            await CreateEventAsync(organizerUserId);

            var userSummary = CreateUser(
                organizerUserId,
                "Organizer",
                ApplicationRoles.Participant,
                ApplicationRoles.Organizer);
            var userManagementService = new FakeUserManagementService(userSummary);
            var handler = new GetUserByIdQueryHandler(DbContext, userManagementService);

            var result = await handler.Handle(
                new GetUserByIdQuery(organizerUserId),
                CancellationToken.None);

            Assert.Equal(organizerUserId, result.Id);
            Assert.Equal(userSummary.FullName, result.FullName);
            Assert.Equal(userSummary.Email, result.Email);
            Assert.Equal(UserStatus.Active.ToString(), result.Status);
            Assert.Equal(userSummary.Roles, result.Roles);
            Assert.Equal(1, result.CreatedEventCount);
        }

        private static UserSummaryResult CreateUser(
            Guid userId,
            string fullName,
            params string[] roles)
        {
            return new UserSummaryResult(
                userId,
                fullName,
                $"{fullName.ToLowerInvariant().Replace(" ", ".")}@example.com",
                UserStatus.Active,
                DateTime.UtcNow,
                DateTime.UtcNow,
                roles);
        }

        private sealed class FakeUserManagementService : IUserManagementService
        {
            public FakeUserManagementService(params UserSummaryResult[] users)
            {
                Users = users.ToDictionary(user => user.UserId);
            }

            public Dictionary<Guid, UserSummaryResult> Users { get; }

            public Task<IReadOnlyList<UserSummaryResult>> ListUsersAsync(
                UserListFilter filter,
                CancellationToken cancellationToken)
            {
                var users = Users.Values.AsEnumerable();

                if (!string.IsNullOrWhiteSpace(filter.Role))
                {
                    users = users.Where(user => user.Roles.Contains(filter.Role));
                }

                if (filter.Status.HasValue)
                {
                    users = users.Where(user => user.Status == filter.Status.Value);
                }

                if (!string.IsNullOrWhiteSpace(filter.Search))
                {
                    users = users.Where(user =>
                        user.FullName.Contains(filter.Search, StringComparison.OrdinalIgnoreCase)
                        || user.Email.Contains(filter.Search, StringComparison.OrdinalIgnoreCase));
                }

                return Task.FromResult<IReadOnlyList<UserSummaryResult>>(users.ToArray());
            }

            public Task<UserSummaryResult?> FindUserSummaryByIdAsync(
                Guid userId,
                CancellationToken cancellationToken)
            {
                return Task.FromResult(
                    Users.TryGetValue(userId, out var user)
                        ? user
                        : null);
            }

            public Task UpdateUserStatusAsync(
                Guid userId,
                UserStatus status,
                CancellationToken cancellationToken)
            {
                Users[userId] = Users[userId] with { Status = status };

                return Task.CompletedTask;
            }
        }

        private sealed class TestCurrentUserService : ICurrentUserService
        {
            private readonly string[] _roles;

            public TestCurrentUserService(Guid? userId, params string[] roles)
            {
                UserId = userId;
                _roles = roles;
            }

            public Guid? UserId { get; }

            public string? Email => null;

            public bool IsAuthenticated => UserId.HasValue;

            public IReadOnlyCollection<string> Roles => _roles;

            public bool IsInRole(string role)
            {
                return _roles.Contains(role);
            }
        }

        private sealed class FakeRefreshTokenRevocationService
            : IRefreshTokenRevocationService
        {
            public List<Guid> RevokedUserIds { get; } = [];

            public Task RevokeAsync(
                string tokenHash,
                string? ipAddress,
                CancellationToken cancellationToken)
            {
                throw new NotSupportedException();
            }

            public Task RevokeAllForUserAsync(
                Guid userId,
                string? ipAddress,
                CancellationToken cancellationToken)
            {
                RevokedUserIds.Add(userId);
                return Task.CompletedTask;
            }
        }

        private sealed class TestClientContextService : IClientContextService
        {
            public string? IpAddress => "127.0.0.1";
        }
    }
}
